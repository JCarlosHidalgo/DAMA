import { ChangeDetectionStrategy, Component, computed, effect, input, output } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatSelectModule } from '@angular/material/select';
import { MatTooltipModule } from '@angular/material/tooltip';

import { ClassGroup } from '@core/models';
import { Icon } from '@shared/design/components/icon';

import { GroupSource, findSelectedGroup } from './group-select.logic';
import { groupSelectStyles } from './group-select.variants';

export type { GroupSource } from './group-select.logic';

@Component({
  selector: 'app-group-select',
  imports: [MatFormFieldModule, MatSelectModule, MatButtonModule, MatTooltipModule, Icon],
  template: `
    <div [class]="styles.root()">
      <mat-form-field appearance="outline" [class]="styles.field()" subscriptSizing="dynamic">
        <mat-label>Grupo</mat-label>
        <mat-select
          [value]="selectedGroupId()"
          (valueChange)="groupChange.emit($event)"
          [disabled]="locked() || loading() || groups().length === 0"
        >
          @for (group of groups(); track group.id) {
            <mat-option [value]="group.id">{{ group.name }}</mat-option>
          }
        </mat-select>
      </mat-form-field>

      @if (editable()) {
        <div [class]="styles.actions()">
          <button
            mat-icon-button
            matTooltip="Nuevo grupo"
            [disabled]="locked() || creating()"
            (click)="createRequested.emit()"
          >
            <app-icon name="plus" />
          </button>
          <button
            mat-icon-button
            matTooltip="Renombrar grupo"
            [disabled]="locked() || !selectedGroup() || renaming()"
            (click)="renameRequested.emit(selectedGroup()!)"
          >
            <app-icon name="edit" />
          </button>
          <button
            mat-icon-button
            matTooltip="Eliminar grupo"
            [class]="styles.dangerButton()"
            [disabled]="locked() || !selectedGroup() || deleting()"
            (click)="deleteRequested.emit(selectedGroup()!)"
          >
            <app-icon name="trash" />
          </button>
        </div>
      }
    </div>
  `,
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class GroupSelect {
  readonly editable = input<boolean>(false);
  readonly locked = input<boolean>(false);
  readonly selectedGroupId = input<string>('');
  readonly source = input<GroupSource>('tenant');
  readonly groups = input<ClassGroup[]>([]);
  readonly loading = input<boolean>(false);
  readonly creating = input<boolean>(false);
  readonly renaming = input<boolean>(false);
  readonly deleting = input<boolean>(false);

  readonly groupChange = output<string>();
  readonly groupsLoaded = output<ClassGroup[]>();
  readonly createRequested = output<void>();
  readonly renameRequested = output<ClassGroup>();
  readonly deleteRequested = output<ClassGroup>();

  protected readonly styles = groupSelectStyles();
  protected readonly selectedGroup = computed<ClassGroup | undefined>(() =>
    findSelectedGroup(this.groups(), this.selectedGroupId()),
  );

  constructor() {
    effect(() => {
      const loaded = this.groups();
      if (loaded.length > 0) {
        this.groupsLoaded.emit(loaded);
      }
    });
  }
}
