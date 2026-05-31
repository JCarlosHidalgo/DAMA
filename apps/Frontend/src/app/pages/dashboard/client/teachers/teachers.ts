import { ChangeDetectionStrategy, Component } from '@angular/core';
import { UserList } from '@pages/dashboard/client/users/user-list';

@Component({
  selector: 'app-teachers',
  imports: [UserList],
  template: `<app-user-list kind="teacher" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Teachers {}
