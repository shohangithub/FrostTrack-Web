import { Component, OnInit } from '@angular/core';
import { RouterLink } from '@angular/router';

@Component({
    selector: 'app-blank',
    templateUrl: './blank.component.html',
    styleUrls: ['./blank.component.sass'],
    standalone: true,
    imports: [RouterLink]
})
export class BlankComponent implements OnInit {

  constructor() { }

  ngOnInit(): void {
  }

}
