import { ChangeDetectionStrategy, Component, inject } from '@angular/core';
import { MatButtonModule } from '@angular/material/button';
import { MatCardModule } from '@angular/material/card';
import { MatTableModule } from '@angular/material/table';
import { MatTabsModule } from '@angular/material/tabs';
import { MatTooltipModule } from '@angular/material/tooltip';
import { Observable } from 'rxjs';

import { PaymentApi } from '@core/api';
import { FailedQrPayment, Page, PendingQrPayment, SuccessQrPayment } from '@core/models';
import { NotificationService } from '@core/services';
import { PaginatedTabState } from '@core/utils';
import { EmptyState, Icon, LoadingSkeleton, PageHead, Paginator } from '@shared/components';
import { MoneyPipe, TenantDatePipe } from '@shared/pipes';

import { TabKind, tabKindForIndex } from './debt-status.logic';
import { debtStatusStyles } from './debt-status.variants';

@Component({
  selector: 'app-debt-status',
  imports: [
    MatCardModule,
    MatTabsModule,
    MatTableModule,
    MatButtonModule,
    MatTooltipModule,
    Icon,
    PageHead,
    Paginator,
    LoadingSkeleton,
    EmptyState,
    MoneyPipe,
    TenantDatePipe,
  ],
  template: `
    <app-page-head title="Estado de deudas" subtitle="Historial de QRs generados." />

    <mat-card [class]="styles.tabsCard()">
      <mat-card-content [class]="styles.cardContent()">
        <mat-tab-group (selectedIndexChange)="onTabChange($event)">
          <mat-tab label="Pendientes">
            <ng-template matTabContent>
              @if (pending.state().loading) {
                <div [class]="styles.skelStack()">
                  <app-loading-skeleton [height]="40" />
                  <app-loading-skeleton [height]="40" />
                  <app-loading-skeleton [height]="40" />
                </div>
              } @else if (!pending.hasItems()) {
                <app-empty-state icon="receipt" message="Sin deudas pendientes." />
              } @else {
                <div [class]="styles.tableWrap()">
                  <table
                    mat-table
                    [dataSource]="pending.state().page!.items"
                    [class]="styles.table()"
                  >
                    <ng-container matColumnDef="quantity">
                      <th mat-header-cell *matHeaderCellDef [class]="styles.num()">Clases</th>
                      <td mat-cell *matCellDef="let payment" [class]="styles.num()">
                        {{ payment.classQuantity }}
                      </td>
                    </ng-container>
                    <ng-container matColumnDef="cost">
                      <th mat-header-cell *matHeaderCellDef [class]="styles.num()">Costo</th>
                      <td mat-cell *matCellDef="let payment" [class]="styles.numMono()">
                        {{ payment.cost | money: payment.currency }}
                      </td>
                    </ng-container>
                    <ng-container matColumnDef="ref">
                      <th mat-header-cell *matHeaderCellDef>Referencia</th>
                      <td mat-cell *matCellDef="let payment" class="dama-mono tabular-nums">
                        {{ payment.externalReference }}
                      </td>
                    </ng-container>
                    <ng-container matColumnDef="qr">
                      <th mat-header-cell *matHeaderCellDef class="mat-column-actions">QR</th>
                      <td mat-cell *matCellDef="let payment" class="mat-column-actions">
                        @if (payment.qrImageUrl) {
                          <a
                            mat-icon-button
                            [href]="payment.qrImageUrl"
                            target="_blank"
                            rel="noopener noreferrer"
                            matTooltip="Abrir QR"
                          >
                            <app-icon name="qr" />
                          </a>
                        }
                      </td>
                    </ng-container>
                    <tr mat-header-row *matHeaderRowDef="pendingColumns"></tr>
                    <tr mat-row *matRowDef="let row; columns: pendingColumns"></tr>
                  </table>
                </div>
                <div [class]="styles.paginatorWrap()">
                  <app-paginator
                    [page]="{
                      currentIndex: pending.state().pageIndex,
                      maxIndex: pending.state().page!.maxIndex,
                    }"
                    (pageChange)="changePage('pending', $event)"
                  />
                </div>
              }
            </ng-template>
          </mat-tab>

          <mat-tab label="Pagadas">
            <ng-template matTabContent>
              @if (success.state().loading) {
                <div [class]="styles.skelStack()">
                  <app-loading-skeleton [height]="40" />
                  <app-loading-skeleton [height]="40" />
                  <app-loading-skeleton [height]="40" />
                </div>
              } @else if (!success.hasItems()) {
                <app-empty-state icon="check" message="Sin pagos confirmados." />
              } @else {
                <div [class]="styles.tableWrap()">
                  <table
                    mat-table
                    [dataSource]="success.state().page!.items"
                    [class]="styles.table()"
                  >
                    <ng-container matColumnDef="quantity">
                      <th mat-header-cell *matHeaderCellDef [class]="styles.num()">Clases</th>
                      <td mat-cell *matCellDef="let payment" [class]="styles.num()">
                        {{ payment.classQuantity }}
                      </td>
                    </ng-container>
                    <ng-container matColumnDef="cost">
                      <th mat-header-cell *matHeaderCellDef [class]="styles.num()">Costo</th>
                      <td mat-cell *matCellDef="let payment" [class]="styles.numMono()">
                        {{ payment.cost | money: payment.currency }}
                      </td>
                    </ng-container>
                    <ng-container matColumnDef="paidAt">
                      <th mat-header-cell *matHeaderCellDef>Pagado</th>
                      <td mat-cell *matCellDef="let payment">
                        {{ payment.paidAt | tenantDate: 'datetime' }}
                      </td>
                    </ng-container>
                    <tr mat-header-row *matHeaderRowDef="successColumns"></tr>
                    <tr mat-row *matRowDef="let row; columns: successColumns"></tr>
                  </table>
                </div>
                <div [class]="styles.paginatorWrap()">
                  <app-paginator
                    [page]="{
                      currentIndex: success.state().pageIndex,
                      maxIndex: success.state().page!.maxIndex,
                    }"
                    (pageChange)="changePage('success', $event)"
                  />
                </div>
              }
            </ng-template>
          </mat-tab>

          <mat-tab label="Fallidas">
            <ng-template matTabContent>
              @if (failed.state().loading) {
                <div [class]="styles.skelStack()">
                  <app-loading-skeleton [height]="40" />
                  <app-loading-skeleton [height]="40" />
                  <app-loading-skeleton [height]="40" />
                </div>
              } @else if (!failed.hasItems()) {
                <app-empty-state icon="ban" message="Sin pagos fallidos." />
              } @else {
                <div [class]="styles.tableWrap()">
                  <table
                    mat-table
                    [dataSource]="failed.state().page!.items"
                    [class]="styles.table()"
                  >
                    <ng-container matColumnDef="quantity">
                      <th mat-header-cell *matHeaderCellDef [class]="styles.num()">Clases</th>
                      <td mat-cell *matCellDef="let payment" [class]="styles.num()">
                        {{ payment.classQuantity }}
                      </td>
                    </ng-container>
                    <ng-container matColumnDef="cost">
                      <th mat-header-cell *matHeaderCellDef [class]="styles.num()">Costo</th>
                      <td mat-cell *matCellDef="let payment" [class]="styles.numMono()">
                        {{ payment.cost | money: payment.currency }}
                      </td>
                    </ng-container>
                    <ng-container matColumnDef="failedAt">
                      <th mat-header-cell *matHeaderCellDef>Fallida</th>
                      <td mat-cell *matCellDef="let payment">
                        {{ payment.failedAt | tenantDate: 'datetime' }}
                      </td>
                    </ng-container>
                    <tr mat-header-row *matHeaderRowDef="failedColumns"></tr>
                    <tr mat-row *matRowDef="let row; columns: failedColumns"></tr>
                  </table>
                </div>
                <div [class]="styles.paginatorWrap()">
                  <app-paginator
                    [page]="{
                      currentIndex: failed.state().pageIndex,
                      maxIndex: failed.state().page!.maxIndex,
                    }"
                    (pageChange)="changePage('failed', $event)"
                  />
                </div>
              }
            </ng-template>
          </mat-tab>
        </mat-tab-group>
      </mat-card-content>
    </mat-card>
  `,
  host: { class: 'block' },
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class DebtStatus {
  private readonly paymentApi = inject(PaymentApi);
  private readonly notifications = inject(NotificationService);

  protected readonly styles = debtStatusStyles();
  protected readonly pendingColumns = ['quantity', 'cost', 'ref', 'qr'];
  protected readonly successColumns = ['quantity', 'cost', 'paidAt'];
  protected readonly failedColumns = ['quantity', 'cost', 'failedAt'];

  protected readonly pending = new PaginatedTabState<PendingQrPayment>();
  protected readonly success = new PaginatedTabState<SuccessQrPayment>();
  protected readonly failed = new PaginatedTabState<FailedQrPayment>();

  constructor() {
    this.loadTab('pending', 0);
  }

  onTabChange(index: number): void {
    const kind = tabKindForIndex(index);
    const tab = this.tabFor(kind);
    if (!tab.state().page) {
      this.loadTab(kind, 0);
    }
  }

  async changePage(kind: TabKind, pageIndex: number): Promise<void> {
    await this.loadTab(kind, pageIndex);
  }

  private tabFor(kind: TabKind) {
    switch (kind) {
      case 'pending':
        return this.pending;
      case 'success':
        return this.success;
      case 'failed':
        return this.failed;
    }
  }

  private async loadTab(kind: TabKind, pageIndex: number): Promise<void> {
    try {
      const tab = this.tabFor(kind);
      await tab.loadFrom(
        (page) => this.fetchPage(kind, page) as Observable<Page<never>>,
        pageIndex,
      );
    } catch {
      this.notifications.error('Error al cargar lista.');
    }
  }

  private fetchPage(kind: TabKind, pageIndex: number) {
    switch (kind) {
      case 'pending':
        return this.paymentApi.listPendingQr(pageIndex);
      case 'success':
        return this.paymentApi.listSuccessQr(pageIndex);
      case 'failed':
        return this.paymentApi.listFailedQr(pageIndex);
    }
  }
}
