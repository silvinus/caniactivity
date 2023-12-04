import { Component, OnInit } from '@angular/core';
import { environment } from '../../../environments/environment';
import { File, FileService } from '../../shared/services/files.service';

@Component({
  selector: 'app-photos',
  templateUrl: './photos.component.html',
  styleUrls: ['./photos.component.scss']
})
export class PhotosComponent implements OnInit {
  photos: File[] = []

  constructor(private filesService: FileService) {

  }

  async ngOnInit() {
    this.filesService.getFiles()
      .then(data => {
        this.photos = data.map(w => {
          w.widthRatio = this.getRandomArbitrary(1, 4);
          w.heightRatio = w.widthRatio;

          return w;
        });
      });
  }

  getRandomArbitrary(min: number, max: number): number {
    return Math.random() * (max - min) + min;
  }

  displayImage(e: any) {
    return `${environment.apiUrl}${e.key}`.replaceAll('\\', '/');
  }

}
