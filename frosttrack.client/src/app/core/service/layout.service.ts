import { Injectable } from '@angular/core';
import { BehaviorSubject } from 'rxjs';
import { Router } from '@angular/router';
import { ROUTES } from 'app/layout/sidebar/sidebar-items';
import { RouteInfo } from 'app/layout/sidebar/sidebar.metadata';
@Injectable({
  providedIn: 'root',
})
export class LayoutService {
  public currentRouteSubject: BehaviorSubject<any[]>;

  constructor(private router: Router) {
    this.currentRouteSubject = new BehaviorSubject<any[]>([
      {
        path: '/dashboard',
        title: 'Dashboard',
      },
    ]);
  }
  private routePath: any = [];

  loadCurrentRoute() {
    const raw_url = this.router.url;
    if (raw_url) {
      const clean_url = raw_url.split('?')[0];
      const url_module = clean_url.split('/').filter((x) => x != '');

      const route = ROUTES.find(
        (x) => x.path == url_module[0] || x.path == clean_url || '/' + x.path == clean_url
      );
      if (route) {
        this.routePath.length = 0;
        this.routePath.push({
          path: '/' + route.path,
          title: route.title,
        });
        if (route.submenu.length > 0) {
          this.deepDragSubMenu(route.submenu, url_module, clean_url, 1);
        }
      }
    }
    this.currentRouteSubject.next(this.routePath);
  }
  private deepDragSubMenu(
    menus: RouteInfo[],
    url_module: string[],
    route: string,
    loop: number
  ) {
    const x = menus.find((x) => x.path == url_module[loop] || x.path == route || x.path == '/' + url_module.slice(0, loop + 1).join('/'));
    if (x) {
      this.routePath.push({ path: x.path, title: x.title });
      if (x.submenu.length > 0) {
        this.deepDragSubMenu(x.submenu, url_module, route, loop + 1);
      }
    }
  }
}
