import { Component, inject } from '@angular/core';
import { RouterLink } from '@angular/router';
import { TranslatePipe } from '../../pipes/translate.pipe';
import { SeoService } from '../../services/seo.service';

@Component({
  selector: 'app-buyers',
  standalone: true,
  imports: [RouterLink, TranslatePipe],
  templateUrl: './buyers.component.html',
  styleUrl: './buyers.component.scss'
})
export class BuyersComponent {
  constructor() {
    inject(SeoService).set({
      title:       'For Buyers',
      description: 'Extend your payment terms and strengthen supplier relationships with Credit Plus. Optimise your working capital using our SCF platform.',
      keywords:    'buyer finance, DPO extension, supply chain finance buyers, working capital, Jordan'
    });
  }
}
