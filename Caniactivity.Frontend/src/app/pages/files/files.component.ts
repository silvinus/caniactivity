import { Component, OnInit } from '@angular/core';
import RemoteFileSystemProvider from 'devextreme/file_management/remote_provider';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-files',
  templateUrl: './files.component.html',
  styleUrls: ['./files.component.scss']
})
export class FilesComponent implements OnInit {
  allowedFileExtensions: string[] = ['.jpg', '.png', '.jpeg'];
  remoteProvider: RemoteFileSystemProvider = new RemoteFileSystemProvider({
    endpointUrl: `${environment.apiUrl}/api/files`,
    beforeSubmit: (options) => {
      console.log(options);
    }
  });
  imageItemToDisplay: any = {};
  popupVisible = false;

  constructor() { }

  ngOnInit(): void {
  }

  displayImagePopup(e: any) {
    this.imageItemToDisplay = `${environment.apiUrl}${e.file.dataItem.key}`;
    this.popupVisible = true;
  }
}
