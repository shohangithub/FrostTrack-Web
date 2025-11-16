import { Component, OnInit } from '@angular/core';
import { FormsModule } from '@angular/forms';

@Component({
    selector: 'app-reset',
    templateUrl: './reset.component.html',
    styleUrls: ['./reset.component.sass'],
    standalone: true,
    imports: [FormsModule]
})
export class ResetComponent implements OnInit {

  constructor() { }

  ngOnInit(): void {
  }

}
