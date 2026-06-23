import { Injectable } from '@angular/core';
import emailjs from '@emailjs/browser';
import { from } from 'rxjs';

const SERVICE_ID  = 'service_uolkdfq';
const TEMPLATE_ID = 'template_fm69ycv';
const PUBLIC_KEY  = 'IQsoPWaN1bdxgbcGI';

export interface ContactPayload {
  name: string;
  email: string;
  company?: string;
  message: string;
}

@Injectable({ providedIn: 'root' })
export class EmailService {
  send(payload: ContactPayload) {
    const params = {
      title:   payload.company ? `${payload.name} — ${payload.company}` : payload.name,
      name:    payload.name,
      email:   payload.email,
      company: payload.company || '—',
      message: payload.message
    };
    return from(emailjs.send(SERVICE_ID, TEMPLATE_ID, params, PUBLIC_KEY));
  }
}
