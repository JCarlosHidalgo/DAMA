import { Injectable, inject } from '@angular/core';
import type { HubConnection } from '@microsoft/signalr';
import { Observable, Subject } from 'rxjs';

import { environment } from '@env/environment';
import { AuthService } from '@core/auth';
import { ScheduledClassAttendance, UniqueClassAttendance } from '@core/models';

type AttendanceMessage = ScheduledClassAttendance | UniqueClassAttendance;
type SignalrModule = typeof import('@microsoft/signalr');

@Injectable({ providedIn: 'root' })
export class AttendanceRealtimeService {
  private readonly authService = inject(AuthService);
  private signalr: SignalrModule | null = null;
  private hubConnection: HubConnection | null = null;
  private startingPromise: Promise<void> | null = null;
  private readonly subjectByGroup = new Map<string, Subject<AttendanceMessage>>();
  private readonly subscriberCountByGroup = new Map<string, number>();

  connectToScheduled(classId: string, classDate: string): Observable<ScheduledClassAttendance> {
    const groupKey = `scheduled:${classId}:${classDate}`;
    return this.subscribe<ScheduledClassAttendance>(
      groupKey,
      (hub) => hub.invoke('JoinScheduledClass', classId, classDate),
      (hub) => hub.invoke('LeaveScheduledClass', classId, classDate),
    );
  }

  connectToUnique(classId: string): Observable<UniqueClassAttendance> {
    const groupKey = `unique:${classId}`;
    return this.subscribe<UniqueClassAttendance>(
      groupKey,
      (hub) => hub.invoke('JoinUniqueClass', classId),
      (hub) => hub.invoke('LeaveUniqueClass', classId),
    );
  }

  private subscribe<T extends AttendanceMessage>(
    groupKey: string,
    join: (hub: HubConnection) => Promise<void>,
    leave: (hub: HubConnection) => Promise<void>,
  ): Observable<T> {
    let groupSubject = this.subjectByGroup.get(groupKey);
    if (!groupSubject) {
      groupSubject = new Subject<AttendanceMessage>();
      this.subjectByGroup.set(groupKey, groupSubject);
    }

    return new Observable<T>((observer) => {
      const subscription = groupSubject!.subscribe((message) => observer.next(message as T));
      const subscriberCount = (this.subscriberCountByGroup.get(groupKey) ?? 0) + 1;
      this.subscriberCountByGroup.set(groupKey, subscriberCount);

      this.ensureStarted()
        .then((hub) => (subscriberCount === 1 ? join(hub) : Promise.resolve()))
        .catch((error) => observer.error(error));

      return () => {
        subscription.unsubscribe();
        const nextCount = (this.subscriberCountByGroup.get(groupKey) ?? 1) - 1;
        if (nextCount <= 0) {
          this.subscriberCountByGroup.delete(groupKey);
          this.subjectByGroup.delete(groupKey);
          if (
            this.hubConnection &&
            this.signalr &&
            this.hubConnection.state === this.signalr.HubConnectionState.Connected
          ) {
            leave(this.hubConnection).catch(() => undefined);
          }
          if (this.subscriberCountByGroup.size === 0) {
            this.stop();
          }
        } else {
          this.subscriberCountByGroup.set(groupKey, nextCount);
        }
      };
    });
  }

  private async ensureStarted(): Promise<HubConnection> {
    if (
      this.hubConnection &&
      this.signalr &&
      this.hubConnection.state === this.signalr.HubConnectionState.Connected
    ) {
      return this.hubConnection;
    }
    if (this.startingPromise) {
      await this.startingPromise;
      return this.hubConnection!;
    }

    const signalr = (this.signalr ??= await import('@microsoft/signalr'));
    const newHubConnection = new signalr.HubConnectionBuilder()
      .withUrl(`${environment.apiBaseUrl}/api/attendance/hubs/attendance`, {
        accessTokenFactory: () => this.authService.accessToken ?? '',
      })
      .withAutomaticReconnect()
      .configureLogging(signalr.LogLevel.Warning)
      .build();

    newHubConnection.on('AttendanceMarked', (incomingMessage: AttendanceMessage) => {
      const scheduledKey = `scheduled:${incomingMessage.classId}:${incomingMessage.classDate}`;
      const uniqueKey = `unique:${incomingMessage.classId}`;
      this.subjectByGroup.get(scheduledKey)?.next(incomingMessage);
      this.subjectByGroup.get(uniqueKey)?.next(incomingMessage);
    });

    newHubConnection.onreconnected(async () => {
      for (const groupKey of this.subscriberCountByGroup.keys()) {
        try {
          if (groupKey.startsWith('scheduled:')) {
            const [, classId, classDate] = groupKey.split(':');
            await newHubConnection.invoke('JoinScheduledClass', classId, classDate);
          } else if (groupKey.startsWith('unique:')) {
            const [, classId] = groupKey.split(':');
            await newHubConnection.invoke('JoinUniqueClass', classId);
          }
        } catch {
          // best-effort re-join; consumers can resubscribe if needed
        }
      }
    });

    this.hubConnection = newHubConnection;
    this.startingPromise = newHubConnection.start();
    try {
      await this.startingPromise;
    } finally {
      this.startingPromise = null;
    }
    return newHubConnection;
  }

  private stop(): void {
    const hubToStop = this.hubConnection;
    this.hubConnection = null;
    if (hubToStop) {
      hubToStop.stop().catch(() => undefined);
    }
  }
}
