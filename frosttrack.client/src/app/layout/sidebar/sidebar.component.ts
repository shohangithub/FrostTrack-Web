import {
  Router,
  NavigationEnd,
  RouterLink,
  RouterLinkActive,
} from '@angular/router';
import { DOCUMENT, NgClass } from '@angular/common';
import {
  Component,
  Inject,
  ElementRef,
  OnInit,
  Renderer2,
  HostListener,
  OnDestroy,
} from '@angular/core';
import { ROUTES } from './sidebar-items';
import { RouteInfo } from './sidebar.metadata';
import { TranslateModule } from '@ngx-translate/core';
import { FeatherModule } from 'angular-feather';
import { NgScrollbar } from 'ngx-scrollbar';
import { AuthService } from '@core';
@Component({
  selector: 'app-sidebar',
  templateUrl: './sidebar.component.html',
  styleUrls: ['./sidebar.component.sass'],
  standalone: true,
  imports: [
    RouterLink,
    NgScrollbar,
    RouterLinkActive,
    NgClass,
    FeatherModule,
    TranslateModule,
  ],
})
export class SidebarComponent implements OnInit, OnDestroy {
  public sidebarItems!: RouteInfo[];
  public innerHeight?: number;
  public bodyTag!: HTMLElement;
  listMaxHeight?: string;
  listMaxWidth?: string;
  userFullName?: string;
  userImg?: string;
  userType?: string;
  headerHeight = 60;
  currentRoute?: string;
  routerObj;
  constructor(
    @Inject(DOCUMENT) private document: Document,
    private renderer: Renderer2,
    public elementRef: ElementRef,
    private authService: AuthService,
    private router: Router
  ) {
    this.routerObj = this.router.events.subscribe((event) => {
      if (event instanceof NavigationEnd) {
        // close sidebar on mobile screen after menu select
        this.renderer.removeClass(this.document.body, 'overlay-open');
        this.sidebbarClose();
      }
    });
  }
  @HostListener('window:resize')
  windowResizecall() {
    if (window.innerWidth < 1025) {
      this.renderer.removeClass(this.document.body, 'side-closed');
    }
    this.setMenuHeight();
    this.checkStatuForResize(false);
  }
  @HostListener('document:mousedown', ['$event'])
  onGlobalClick(event: Event): void {
    if (!this.elementRef.nativeElement.contains(event.target)) {
      this.renderer.removeClass(this.document.body, 'overlay-open');
      this.sidebbarClose();
    }
  }
  callToggleMenu(event: Event, length: number) {
    if (length > 0) {
      const parentElement = (event.target as HTMLInputElement).closest('li');
      const activeClass = parentElement?.classList.contains('active');

      if (activeClass) {
        this.renderer.removeClass(parentElement, 'active');
      } else {
        this.renderer.addClass(parentElement, 'active');
      }
    }
  }

  /**
   * Filter menu items based on user roles
   * @param menuItems - Array of menu items to filter
   * @param userRoles - Array of user's roles
   * @returns Filtered menu items
   */
  private filterMenuByRole(
    menuItems: RouteInfo[],
    userRoles: string[]
  ): RouteInfo[] {
    return menuItems.filter((item) => {
      // Check if item has role restrictions
      if (item.allowedRoles && item.allowedRoles.length > 0) {
        // Check if user has any of the allowed roles
        const hasAccess = item.allowedRoles.some((role) =>
          userRoles.includes(role)
        );
        if (!hasAccess) {
          return false; // User doesn't have required role
        }
      }

      // Filter submenu items recursively
      if (item.submenu && item.submenu.length > 0) {
        item.submenu = this.filterMenuByRole(item.submenu, userRoles);
        // Hide parent if all children are filtered out
        if (item.submenu.length === 0 && !item.path) {
          return false;
        }
      }

      return true; // Item is accessible
    });
  }
  ngOnInit() {
    if (this.authService.currentUserValue) {
      debugger;
      // Get user roles from JWT token
      const userRoles = this.authService.getUserRoles();
      const userRoleArray = Array.isArray(userRoles) ? userRoles : [userRoles];

      // Filter menu items based on user roles
      this.sidebarItems = this.filterMenuByRole(ROUTES, userRoleArray);

      // Extract user information from JWT token
      const decodedToken = this.authService.getDecodedToken();
      if (decodedToken) {
        this.userFullName =
          decodedToken['unique_name'] || decodedToken['name'] || 'User';
        this.userImg =
          decodedToken['profileImageUrl'] || 'assets/images/user.png';
        this.userType = decodedToken['role'] || 'User';
      }
    }
    this.initLeftSidebar();
    this.bodyTag = this.document.body;
  }
  ngOnDestroy() {
    this.routerObj.unsubscribe();
  }
  initLeftSidebar() {
    // eslint-disable-next-line @typescript-eslint/no-this-alias
    const _this = this;
    // Set menu height
    _this.setMenuHeight();
    _this.checkStatuForResize(true);
  }
  setMenuHeight() {
    this.innerHeight = window.innerHeight;
    const height = this.innerHeight - this.headerHeight;
    this.listMaxHeight = height + '';
    this.listMaxWidth = '500px';
  }
  isOpen() {
    return this.bodyTag.classList.contains('overlay-open');
  }
  // eslint-disable-next-line @typescript-eslint/no-unused-vars
  checkStatuForResize(firstTime: boolean) {
    if (window.innerWidth < 1025) {
      this.renderer.addClass(this.document.body, 'sidebar-gone');
    } else {
      this.renderer.removeClass(this.document.body, 'sidebar-gone');
    }
  }
  mouseHover() {
    const body = this.elementRef.nativeElement.closest('body');
    if (body.classList.contains('submenu-closed')) {
      this.renderer.addClass(this.document.body, 'side-closed-hover');
      this.renderer.removeClass(this.document.body, 'submenu-closed');
    }
  }
  mouseOut() {
    const body = this.elementRef.nativeElement.closest('body');
    if (body.classList.contains('side-closed-hover')) {
      this.renderer.removeClass(this.document.body, 'side-closed-hover');
      this.renderer.addClass(this.document.body, 'submenu-closed');
    }
  }

  sidebbarClose() {
    if (window.innerWidth < 1025) {
      this.renderer.addClass(this.document.body, 'sidebar-gone');
    }
  }
}
