import { Component } from '@angular/core';
import { ConvertComponent } from './convert/convert.component';

@Component({
  imports: [ConvertComponent],
  selector: 'app-root',
  template: '<app-convert />',
})
export class App {}
