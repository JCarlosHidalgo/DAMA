import {
  faGraduationCap,
  faUsers,
  faCalendarDays,
  faQrcode,
  faEye,
  faEyeSlash,
  faCircleCheck,
  faTriangleExclamation,
  faBan,
  faPenToSquare,
  faTrash,
  faPlus,
  faUserPlus,
  faRightFromBracket,
  faBars,
  faChevronLeft,
  faChevronRight,
  faImage,
  faGauge,
  faCreditCard,
  faReceipt,
  faMoneyBill1,
  faChalkboardUser,
  faCalendarCheck,
  faBuilding,
  faLayerGroup,
  faRightLeft,
  faClock,
  faSun,
  faMoon,
  type IconDefinition,
} from '@fortawesome/free-solid-svg-icons';

export type IconName =
  | 'graduation-cap'
  | 'users'
  | 'calendar'
  | 'qr'
  | 'eye'
  | 'eye-off'
  | 'check'
  | 'warning'
  | 'ban'
  | 'edit'
  | 'trash'
  | 'plus'
  | 'user-plus'
  | 'logout'
  | 'bars'
  | 'chevron-left'
  | 'chevron-right'
  | 'image'
  | 'gauge'
  | 'credit-card'
  | 'receipt'
  | 'money-bill'
  | 'chalkboard'
  | 'calendar-check'
  | 'building'
  | 'layer-group'
  | 'transfer'
  | 'clock'
  | 'sun'
  | 'moon';

export const ICON_REGISTRY: Record<IconName, IconDefinition> = {
  'graduation-cap': faGraduationCap,
  users: faUsers,
  calendar: faCalendarDays,
  qr: faQrcode,
  eye: faEye,
  'eye-off': faEyeSlash,
  check: faCircleCheck,
  warning: faTriangleExclamation,
  ban: faBan,
  edit: faPenToSquare,
  trash: faTrash,
  plus: faPlus,
  'user-plus': faUserPlus,
  logout: faRightFromBracket,
  bars: faBars,
  'chevron-left': faChevronLeft,
  'chevron-right': faChevronRight,
  image: faImage,
  gauge: faGauge,
  'credit-card': faCreditCard,
  receipt: faReceipt,
  'money-bill': faMoneyBill1,
  chalkboard: faChalkboardUser,
  'calendar-check': faCalendarCheck,
  building: faBuilding,
  'layer-group': faLayerGroup,
  transfer: faRightLeft,
  clock: faClock,
  sun: faSun,
  moon: faMoon,
};
