import { Component, inject } from '@angular/core';
import { TranslatePipe } from '../../pipes/translate.pipe';
import { SeoService } from '../../services/seo.service';

@Component({
  selector: 'app-suppliers',
  standalone: true,
  imports: [TranslatePipe],
  templateUrl: './suppliers.component.html',
  styleUrl: './suppliers.component.scss'
})
export class SuppliersComponent {
  constructor() {
    inject(SeoService).set({
      title:       'For Suppliers',
      description: 'Get paid early on your invoices with Credit Plus Supply Chain Finance. No collateral, fast approval, and zero disruption to your buyer relationships.',
      keywords:    'supplier finance, early payment, invoice financing, Jordan suppliers, SCF'
    });
  }
}
