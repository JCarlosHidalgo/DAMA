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
import { MoneyPipe } from '@shared/pipes';

const DEBT_TEMPLATES_QUERY_KEY = ['debt-templates'] as const;

interface TemplateDialogData {
  mode: 'create' | 'edit';
  initial?: Partial<TemplateDialogResult>;
}

interface TemplateDialogResult {
  description: string;
  classQuantity: number;
  cost: number;
}

@Component({
  selector: 'app-debt-template-dialog',
  imports: [
    ReactiveFormsModule,
    MatDialogModule,
    MatFormFieldModule,
    MatInputModule,
    MatButtonModule,
  ],
  template: `
    <h2 mat-dialog-title>
      {{ data.mode === 'create' ? 'Nueva plantilla' : 'Editar plantilla' }}
    </h2>
    <mat-dialog-content>
      <form [formGroup]="form" class="form">
        <mat-form-field appearance="outline">
          <mat-label>Descripción</mat-label>
          <input matInput formControlName="description" autocomplete="off" />
        </mat-form-field>

        <mat-form-field appearance="outline">
          <mat-label>Cantidad de clases</mat-label>
          <input matInput type="number" min="1" formControlName="classQuantity" />
        </mat-form-field>

        <mat-form-field appearance="outline">
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
  styles: `
    .form {
      display: flex;
      flex-direction: column;
      gap: 12px;
      min-width: 320px;
    }
    mat-form-field {
      width: 100%;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DebtTemplateDialog {
  private readonly formBuilder = inject(FormBuilder);
  readonly dialogRef = inject(MatDialogRef<DebtTemplateDialog, TemplateDialogResult>);
  readonly data = inject<TemplateDialogData>(MAT_DIALOG_DATA);

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
        <app-icon name="plus" /><span class="btn-label">Nueva plantilla</span>
      </button>
    </app-page-head>

    <mat-card class="list-card">
      <mat-card-content>
        @if (templatesQuery.isPending()) {
          <div class="skel-stack">
            <app-loading-skeleton [height]="40" />
            <app-loading-skeleton [height]="40" />
            <app-loading-skeleton [height]="40" />
          </div>
        } @else if (templates().length === 0) {
          <app-empty-state icon="receipt" message="No hay plantillas." />
        } @else {
          <div class="table-wrap">
            <table mat-table [dataSource]="templates()" class="full">
              <ng-container matColumnDef="description">
                <th mat-header-cell *matHeaderCellDef>Descripción</th>
                <td mat-cell *matCellDef="let template">{{ template.description }}</td>
              </ng-container>
              <ng-container matColumnDef="classes">
                <th mat-header-cell *matHeaderCellDef class="num">Clases</th>
                <td mat-cell *matCellDef="let template" class="num">
                  <app-tag variant="primary">{{ template.classQuantity }}</app-tag>
                </td>
              </ng-container>
              <ng-container matColumnDef="cost">
                <th mat-header-cell *matHeaderCellDef class="num">Costo</th>
                <td mat-cell *matCellDef="let template" class="num tnum">
                  {{ template.cost | money }}
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
                    class="danger-btn"
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
  styles: `
    :host {
      display: block;
    }
    .list-card {
      padding: 0;
    }
    .list-card mat-card-content {
      padding: 0;
    }
    .skel-stack {
      display: flex;
      flex-direction: column;
      gap: 12px;
      padding: 20px;
    }
    .table-wrap {
      overflow-x: auto;
    }
    .full {
      width: 100%;
    }
    .num {
      text-align: right;
    }
    .danger-btn {
      color: var(--dama-danger);
    }
    .btn-label {
      margin-left: 6px;
    }
  `,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DebtTemplates {
  private readonly paymentApi = inject(PaymentApi);
  private readonly dialogs = inject(DialogService);
  private readonly notifications = inject(NotificationService);
  private readonly queryClient = inject(QueryClient);

  protected readonly columns = ['description', 'classes', 'cost', 'actions'];

  protected readonly templatesQuery = injectQuery(() => ({
    queryKey: DEBT_TEMPLATES_QUERY_KEY,
    queryFn: () => firstValueFrom(this.paymentApi.listDebtTemplates()),
  }));

  protected readonly templates = computed<DebtTemplate[]>(() => this.templatesQuery.data() ?? []);
  protected readonly subtitle = computed(() => `${this.templates().length} plantilla(s)`);

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
    if (!result) {
      return;
    }
    this.createTemplate.mutate({
      description: result.description,
      classQuantity: result.classQuantity,
      cost: result.cost,
    });
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
    if (!result) {
      return;
    }
    this.updateTemplate.mutate({
      id: template.id,
      payload: {
        description: result.description,
        classQuantity: result.classQuantity,
        cost: result.cost,
      },
    });
  }

  async onDelete(template: DebtTemplate): Promise<void> {
    const confirmed = await this.dialogs.confirm({
      title: 'Eliminar plantilla',
      message: `¿Eliminar plantilla "${template.description}"?`,
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
