import { Component, OnInit } from '@angular/core';

@Component({
  selector: 'app-tarifs',
  templateUrl: './tarifs.component.html',
  styleUrls: ['./tarifs.component.scss']
})
export class TarifsComponent implements OnInit {

  constructor() { }

  ngOnInit(): void {
  }
  screen: Function = (width: any) => {
    return (width < 1200) ? 'sm' : 'lg';
  }
}
