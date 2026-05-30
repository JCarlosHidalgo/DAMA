export interface StudentRemainClasses {
  tenantId: string;
  id: string;
  numberOfClasses: number;
  studentName: string | null;
}

export interface ScheduledClassAttendance {
  tenantId: string;
  classId: string;
  classDate: string;
  startTime: string;
  endTime: string;
  courseName: string;
  studentId: string;
  studentName: string;
}

export interface UniqueClassAttendance {
  tenantId: string;
  classId: string;
  classDate: string;
  startTime: string;
  endTime: string;
  courseName: string;
  studentId: string;
  studentName: string;
}

export interface ScheduledAttendancePayload {
  classId: string;
  courseName: string;
}

export interface UniqueAttendancePayload {
  classId: string;
  courseName: string;
}

export interface ClientIncrementRemainPayload {
  requestId: string;
  quantity: number;
  studentName?: string | null;
}
