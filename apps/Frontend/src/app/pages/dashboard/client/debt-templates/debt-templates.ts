import { ChangeDetectionStrategy, Component, computed, effect, inject } from '@angular/core';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatDialogModule, MatDialogRef, MAT_DIALOG_DATA } from '@angular/material/dialog';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatTableModule } from '@angular/material/table';
import { MatTooltipModule } from '@angular/material/tooltip';
import { QueryClient } from '@tanstack/query-core';
import { injectMutation, injectQuery } from '@tanstack/angular-query-experimental';
import { firstValueFrom } from 'rxjs';

import { PaymentApi } from '@core/api';
import { CreateDebtTemplatePayload, DebtTemplate, UpdateDebtTemplatePayload } from '@core/models';
import { DialogService, NotificationService } from '@core/services';
import { EmptyState, Icon, LoadingSkeleton, PageHead, Tag } from '@shared/components';
import { NoPasswordManager } from '@shared/directives';
import { MoneyPipe } from '@shared/pipes';

import {
  deleteTemplateConfirmMessage,
  debtTemplatesSubtitle,
  resolveTemplateSubmit,
  TemplateDialogResult,
} from './debt-templates.logic';
import { debtTemplateDialogStyles, debtTemplatesStyles } from './debt-templates.variants';

const DEBT_TEMPLATES_QUERY_KEY = ['debt-templates'] as const;

interface TemplateDialogData {
  mode: 'create' | 'edit';
  initial?: Partial<TemplateDialogResult>;
}

@Component({
  selector: 'app-debt-template-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
    NoPasswordManager,
  ],
  template: `
    <h2 mat-dialog-title>
      {{ data.mode === 'create' ? 'Nueva plantilla' : 'Editar plantilla' }}
    </h2>
    <mat-dialog-content>
      <form [formGroup]="form" [class]="styles.form()">
        <mat-form-field appearance="outline" [class]="styles.field()">
          <mat-label>Descripción</mat-label>
          <input matInput formControlName="description" autocomplete="off" />
        </mat-form-field>

        <mat-form-field appearance="outline" [class]="styles.field()">
          <mat-label>Cantidad de clases</mat-label>
          <input matInput type="number" min="1" formControlName="classQuantity" />
        </mat-form-field>

        <mat-form-field appearance="outline" [class]="styles.field()">
          <mat-label>Costo (Bs)</mat-label>
          <input matInput type="number" min="1" formControlName="cost" />
        </mat-form-field>
      </form>
    </mat-dialog-content>
    <mat-dialog-actions align="end">
      <button mat-button (click)="dialogRef.close()">Cancelar</button>
      <button
        mat-flat-button
        color="primary"
        [disabled]="form.invalid"
        (click)="dialogRef.close(form.getRawValue())"
      >
        Guardar
      </button>
    </mat-dialog-actions>
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DebtTemplateDialog {
  private readonly formBuilder = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<DebtTemplateDialog, TemplateDialogResult>);
  readonly data = inject<TemplateDialogData>(MAT_DIALOG_DATA);

  protected readonly styles = debtTemplateDialogStyles();

  protected readonly form = this.formBuilder.nonNullable.group({
    description: [
      this.data.initial?.description ?? '',
      [Validators.required, Validators.maxLength(256)],
    ],
    classQuantity: [
      this.data.initial?.classQuantity ?? 1,
      [Validators.required, Validators.min(1)],
    ],
    cost: [this.data.initial?.cost ?? 1, [Validators.required, Validators.min(1)]],
  });
}

@Component({
  selector: 'app-debt-templates',
  imports: [
    MatCardModule,
    MatTableModule,
    MatButtonModule,
    MatTooltipModule,
    Icon,
    PageHead,
    LoadingSkeleton,
    EmptyState,
    Tag,
    MoneyPipe,
  ],
  template: `
    <app-page-head title="Plantillas de cobro" [subtitle]="subtitle()">
      <button actions mat-flat-button color="primary" (click)="onCreate()">
        <app-icon name="plus" /><span [class]="styles.buttonLabel()">Nueva plantilla</span>
      </button>
    </app-page-head>

    <mat-card [class]="styles.listCard()">
      <mat-card-content [class]="styles.cardContent()">
        @if (templatesQuery.isPending()) {
          <div [class]="styles.skelStack()">
            <app-loading-skeleton [height]="40" />
            <app-loading-skeleton [height]="40" />
            <app-loading-skeleton [height]="40" />
          </div>
        } @else if (templates().length === 0) {
          <app-empty-state icon="receipt" message="No hay plantillas." />
        } @else {
          <div [class]="styles.tableWrap()">
            <table mat-table [dataSource]="templates()" [class]="styles.table()">
              <ng-container matColumnDef="description">
                <th mat-header-cell *matHeaderCellDef>Descripción</th>
                <td mat-cell *matCellDef="let template">{{ template.description }}</td>
              </ng-container>
              <ng-container matColumnDef="classes">
                <th mat-header-cell *matHeaderCellDef [class]="styles.num()">Clases</th>
                <td mat-cell *matCellDef="let template" [class]="styles.num()">
                  <app-tag variant="primary">{{ template.classQuantity }}</app-tag>
                </td>
              </ng-container>
              <ng-container matColumnDef="cost">
                <th mat-header-cell *matHeaderCellDef [class]="styles.num()">Costo</th>
                <td mat-cell *matCellDef="let template" [class]="styles.numMono()">
                  {{ template.cost | money: template.currency }}
                </td>
              </ng-container>
              <ng-container matColumnDef="actions">
                <th mat-header-cell *matHeaderCellDef class="mat-column-actions">Acciones</th>
                <td mat-cell *matCellDef="let template" class="mat-column-actions">
                  <button mat-icon-button matTooltip="Editar" (click)="onEdit(template)">
                    <app-icon name="edit" />
                  </button>
                  <button
                    mat-icon-button
                    matTooltip="Eliminar"
                    [class]="styles.dangerButton()"
                    (click)="onDelete(template)"
                  >
                    <app-icon name="trash" />
                  </button>
                </td>
              </ng-container>
              <tr mat-header-row *matHeaderRowDef="columns"></tr>
              <tr mat-row *matRowDef="let row; columns: columns"></tr>
            </table>
          </div>
        }
      </mat-card-content>
    </mat-card>
  `,
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DebtTemplates {
  private readonly paymentApi = inject(PaymentApi);
  private readonly dialogs = inject(DialogService);
  private readonly notifications = inject(NotificationService);
  private readonly queryClient = inject(QueryClient);

  protected readonly styles = debtTemplatesStyles();
  protected readonly columns = ['description', 'classes', 'cost', 'actions'];

  protected readonly templatesQuery = injectQuery(() => ({
    queryKey: DEBT_TEMPLATES_QUERY_KEY,
    queryFn: () => firstValueFrom(this.paymentApi.listDebtTemplates()),
  }));

  protected readonly templates = computed<DebtTemplate[]>(() => this.templatesQuery.data() ?? []);
  protected readonly subtitle = computed(() => debtTemplatesSubtitle(this.templates().length));

  private readonly createTemplate = injectMutation(() => ({
    mutationFn: (payload: CreateDebtTemplatePayload) =>
      firstValueFrom(this.paymentApi.createDebtTemplate(payload)),
    onSuccess: () => {
      this.notifications.success('Plantilla creada.');
      this.queryClient.invalidateQueries({ queryKey: DEBT_TEMPLATES_QUERY_KEY });
    },
    onError: () => this.notifications.error('Error al crear plantilla.'),
  }));

  private readonly updateTemplate = injectMutation(() => ({
    mutationFn: (input: { id: string; payload: UpdateDebtTemplatePayload }) =>
      firstValueFrom(this.paymentApi.updateDebtTemplate(input.id, input.payload)),
    onSuccess: () => {
      this.notifications.success('Plantilla actualizada.');
      this.queryClient.invalidateQueries({ queryKey: DEBT_TEMPLATES_QUERY_KEY });
    },
    onError: () => this.notifications.error('Error al actualizar plantilla.'),
  }));

  private readonly deleteTemplate = injectMutation(() => ({
    mutationFn: (templateId: string) =>
      firstValueFrom(this.paymentApi.deleteDebtTemplate(templateId)),
    onSuccess: () => {
      this.notifications.success('Plantilla eliminada.');
      this.queryClient.invalidateQueries({ queryKey: DEBT_TEMPLATES_QUERY_KEY });
    },
    onError: () => this.notifications.error('Error al eliminar plantilla.'),
  }));

  constructor() {
    effect(() => {
      if (this.templatesQuery.isError()) {
        this.notifications.error('Error al cargar plantillas.');
      }
    });
  }

  async onCreate(): Promise<void> {
    const result = await this.openTemplateDialog({ mode: 'create' });
    const outcome = resolveTemplateSubmit(result);
    if (outcome.kind === 'skip') {
      return;
    }
    this.createTemplate.mutate(outcome.payload);
  }

  async onEdit(template: DebtTemplate): Promise<void> {
    const result = await this.openTemplateDialog({
      mode: 'edit',
      initial: {
        description: template.description,
        classQuantity: template.classQuantity,
        cost: template.cost,
      },
    });
    const outcome = resolveTemplateSubmit(result);
    if (outcome.kind === 'skip') {
      return;
    }
    this.updateTemplate.mutate({ id: template.id, payload: outcome.payload });
  }

  async onDelete(template: DebtTemplate): Promise<void> {
    const confirmed = await this.dialogs.confirm({
      title: 'Eliminar plantilla',
      message: deleteTemplateConfirmMessage(template.description),
      destructive: true,
      confirmLabel: 'Eliminar',
    });
    if (!confirmed) {
      return;
    }
    this.deleteTemplate.mutate(template.id);
  }

  private openTemplateDialog(data: TemplateDialogData): Promise<TemplateDialogResult | undefined> {
    return this.dialogs.openForm<DebtTemplateDialog, TemplateDialogData, TemplateDialogResult>(
      DebtTemplateDialog,
      data,
      { width: '460px' },
    );
  }
}
