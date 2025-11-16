import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
    selector: 'app-second2',
    templateUrl: './second2.component.html',
    styleUrls: ['./second2.component.sass'],
    standalone: true,
    imports: [RouterLink]
})
export class Second2Component implements OnInit {

  constructor() { }

  ngOnInit(): void {
  }

}
