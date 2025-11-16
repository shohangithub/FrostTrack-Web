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
    const route_url = this.router.url;
    if (route_url) {
      const url_module = route_url.split('/').filter((x) => x != '');

      const route = ROUTES.find(
        (x) => x.path == url_module[0] || x.path == route_url
      );
      if (route) {
        this.routePath.length = 0;
        this.routePath.push({
          path: '/' + route.path,
          title: route.title,
        });
        if (route.submenu.length > 0) {
          this.deepDragSubMenu(route.submenu, url_module, route_url, 1);
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
    const x = menus.find((x) => x.path == url_module[loop] || x.path == route);
    if (x) {
      this.routePath.push({ path: x.path, title: x.title });
      if (x.submenu.length > 0) {
        this.deepDragSubMenu(x.submenu, url_module, route, loop + 1);
      }
    }
  }
}
