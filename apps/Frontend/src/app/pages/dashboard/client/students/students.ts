import { ChangeDetectionStrategy, Component } from '@angular/core';
import { UserList } from '@pages/dashboard/client/users/user-list';

@Component({
  selector: 'app-students',
  imports: [UserList],
  template: `<app-user-list kind="student" />`,
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Students {}
