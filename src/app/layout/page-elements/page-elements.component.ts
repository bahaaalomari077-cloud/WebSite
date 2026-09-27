import { Component, Input, inject } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { CmsContentService } from '../../services/cms-content.service';

@Component({
  selector: 'app-page-elements',
  standalone: true,
  imports: [RouterLink],
  templateUrl: './page-elements.component.html',
  styleUrl: './page-elements.component.scss'
})
export class PageElementsComponent {
  @Input() placement: 'top' | 'bottom' = 'bottom';

  readonly cms = inject(CmsContentService);
  private readonly router = inject(Router);

  get items() {
    return this.cms.pagePlacementsFor(this.router.url, this.placement);
  }
}
