import { DOCUMENT, NgClass } from '@angular/common';
import {
  Component,
  Inject,
  ElementRef,
  OnInit,
  Renderer2,
  AfterViewInit,
} from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { TranslateModule } from '@ngx-translate/core';
import { NgScrollbar } from 'ngx-scrollbar';
import {
  NgbDropdown,
  NgbDropdownToggle,
  NgbDropdownMenu,
} from '@ng-bootstrap/ng-bootstrap';
import { FormsModule, ReactiveFormsModule, UntypedFormBuilder, UntypedFormControl, UntypedFormGroup } from '@angular/forms';
import { FeatherModule } from 'angular-feather';
import {
  AuthService,
  InConfiguration,
  LanguageService,
  RightSidebarService,
} from '@core';
import { ConfigService } from '@config/config.service';
import { LayoutService } from '@core/service/layout.service';
import { BranchService } from 'app/administration/services/branch.service';
import { Subject, pairwise, startWith } from 'rxjs';
import { ILookup } from '@core/models/lookup';
import { ErrorResponse, formatErrorMessage } from 'app/utils/server-error-handler';
import Swal from 'sweetalert2';
import { SwalConfirm } from 'app/theme-config';

@Component({
  selector: 'app-header',
  templateUrl: './header.component.html',
  styleUrls: ['./header.component.sass'],
  standalone: true,
  imports: [
    FeatherModule,
    FormsModule,
    ReactiveFormsModule,
    NgbDropdown,
    NgbDropdownToggle,
    NgbDropdownMenu,
    NgClass,
    NgScrollbar,
    RouterLink,
    TranslateModule,
  ],
  providers: [RightSidebarService,AuthService],
})
export class HeaderComponent implements OnInit, AfterViewInit {
  public config!: InConfiguration;
  isNavbarCollapsed = true;
  flagvalue: string | string[] | undefined;
  countryName: string | string[] = [];
  langStoreValue?: string;
  defaultFlag?: string;
  isOpenSidebar?: boolean;
  docElement?: HTMLElement;
  isFullScreen = false;
  currentRoutes: any[] = [];
  branchForm!: UntypedFormGroup;
  branchs: ILookup<number>[] = [];
  selectedBranch!: number;
  isMainBranch: boolean = false;
  private branchSubject: Subject<number> = new Subject<number>();

  constructor(
    @Inject(DOCUMENT) private document: Document,
    private renderer: Renderer2,
    public elementRef: ElementRef,
    private rightSidebarService: RightSidebarService,
    private configService: ConfigService,
    public authService: AuthService,
    private layoutService: LayoutService,
    private router: Router,
    public languageService: LanguageService,
    private branchService: BranchService,
    private fb: UntypedFormBuilder,
  ) {
  }

  listLang = [
    { text: 'English', flag: 'assets/images/flags/us.jpg', lang: 'en' },
    { text: 'Spanish', flag: 'assets/images/flags/spain.jpg', lang: 'es' },
    { text: 'German', flag: 'assets/images/flags/germany.jpg', lang: 'de' },
  ];

  previousValue!: string
  ngOnInit() {
    this.branchForm = this.fb.group({
      branchId: [this.selectedBranch],
    });
    this.config = this.configService.configData;
    this.docElement = document.documentElement;

    this.langStoreValue = localStorage.getItem('lang') as string;
    const val = this.listLang.filter((x) => x.lang === this.langStoreValue);
    this.countryName = val.map((element) => element.text);
    if (val.length === 0) {
      if (this.flagvalue === undefined) {
        this.defaultFlag = 'assets/images/flags/us.jpg';
      }
    } else {
      this.flagvalue = val.map((element) => element.flag);
    }

    this.fetchBranchLookup();
    this.getUserBranch();

    this.layoutService.currentRouteSubject.subscribe({
      next: (routes) => {
        this.currentRoutes = routes;
      },
      error: (err) => {
        console.log(err);
      },
    });

    this.branchSubject.subscribe((value: number) => {
      if (value) {
        this.isMainBranch = true;
        this.branchForm.get('branchId')?.setValue(value);
        this.selectedBranch = value;
      }
    });

    this.branchForm.get('branchId')?.valueChanges.pipe(startWith(null), pairwise()).subscribe(([prev, next]: [any, any]) => {
      if (prev != null && prev != next && this.previousValue != next) {
        Swal.fire({
          title: "Do you want to change current branch ?",
          showCancelButton: true,
          confirmButtonColor: SwalConfirm.confirmButtonColor,
          cancelButtonColor: SwalConfirm.cancelButtonColor,
          confirmButtonText: 'Yes',
        }).then((result) => {
          if (result.isConfirmed) {
            this.previousValue = "";
            this.branchSubject.next(next)
            this.authService.setUserBranchId(next)
          } else if (result.isDismissed) {
            this.previousValue = prev;
            this.branchSubject.next(prev)
            this.authService.setUserBranchId(prev)
          }
        })
      }
    });
  }
  ngAfterViewInit() {
    // set theme on startup
    if (localStorage.getItem('theme')) {
      this.renderer.removeClass(this.document.body, this.config.layout.variant);
      this.renderer.addClass(
        this.document.body,
        localStorage.getItem('theme') as string
      );
    } else {
      this.renderer.addClass(this.document.body, this.config.layout.variant);
    }

    if (localStorage.getItem('menuOption')) {
      this.renderer.addClass(
        this.document.body,
        localStorage.getItem('menuOption') as string
      );
    } else {
      this.renderer.addClass(
        this.document.body,
        this.config.layout.sidebar.backgroundColor + '-sidebar'
      );
    }

    if (localStorage.getItem('sidebar_status')) {
      if (localStorage.getItem('sidebar_status') === 'close') {
        this.renderer.addClass(this.document.body, 'side-closed');
        this.renderer.addClass(this.document.body, 'submenu-closed');
      } else {
        this.renderer.removeClass(this.document.body, 'side-closed');
        this.renderer.removeClass(this.document.body, 'submenu-closed');
      }
    } else {
      if (this.config.layout.sidebar.collapsed === true) {
        this.renderer.addClass(this.document.body, 'side-closed');
        this.renderer.addClass(this.document.body, 'submenu-closed');
      }
    }
  }
  callFullscreen() {
    if (!this.isFullScreen) {
      if (this.docElement?.requestFullscreen != null) {
        this.docElement?.requestFullscreen();
      }
    } else {
      document.exitFullscreen();
    }
    this.isFullScreen = !this.isFullScreen;
  }
  setLanguage(text: string, lang: string, flag: string) {
    this.countryName = text;
    this.flagvalue = flag;
    this.langStoreValue = lang;
    this.languageService.setLanguage(lang);
  }
  mobileMenuSidebarOpen(event: Event, className: string) {
    if (window.innerWidth < 1025) {
      const hasClass = (event.target as HTMLInputElement).classList.contains(
        className
      );
      if (hasClass) {
        this.renderer.removeClass(this.document.body, className);
        this.renderer.addClass(this.document.body, 'sidebar-gone');
      } else {
        this.renderer.addClass(this.document.body, className);
        this.renderer.removeClass(this.document.body, 'sidebar-gone');
      }
    } else {
      const hasClass = this.document.body.classList.contains('side-closed');
      if (hasClass) {
        this.renderer.removeClass(this.document.body, 'side-closed');
        this.renderer.removeClass(this.document.body, 'submenu-closed');
      } else {
        this.renderer.addClass(this.document.body, 'side-closed');
        this.renderer.addClass(this.document.body, 'submenu-closed');
      }
    }
  }
  public toggleRightSidebar(): void {
    this.rightSidebarService.sidebarState.subscribe((isRunning) => {
      this.isOpenSidebar = isRunning;
    });

    this.rightSidebarService.setRightSidebar(
      (this.isOpenSidebar = !this.isOpenSidebar)
    );
  }
  logout() {
    this.authService.logout().subscribe((res) => {
      if (!res.success) {
        this.router.navigate(['/authentication/signin']);
      }
    });
  }

  fetchBranchLookup() {
    this.branchService.getLookup().subscribe({
      next: (response: ILookup<number>[]) => {
        this.branchs = response;
        this.branchSubject.next(this.selectedBranch);
      },
      error: (err: ErrorResponse) => {
        console.log(formatErrorMessage(err));
      },
    });
  }
  getUserBranch() {
    this.selectedBranch = this.authService.currentBranchId;
  }


}
