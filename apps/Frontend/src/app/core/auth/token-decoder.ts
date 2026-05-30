import { Injectable } from '@angular/core';
import { jwtDecode } from 'jwt-decode';

import { JwtClaims, mapClaims } from './jwt.model';

@Injectable({ providedIn: 'root' })
export class TokenDecoder {
  decode(token: string): JwtClaims | null {
    try {
      return mapClaims(jwtDecode(token));
    } catch {
      return null;
    }
  }
}
