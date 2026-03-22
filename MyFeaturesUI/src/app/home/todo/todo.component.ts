import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, Input, inject } from '@angular/core';
import { TranslateService } from '@ngx-translate/core';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DragDropModule } from 'primeng/dragdrop';
import { DialogService } from 'primeng/dynamicdialog';
import { TableModule } from 'primeng/table';
import { Observable } from 'rxjs';
import { TaskOccurrenceDto } from '../../../infrastructure';
import { TaskTemplateExtendedService } from '../../extended-services/task-template-extended-service';
import { APPLICATION_JSON } from '../../shared/constants';
import { EditTaskDialogComponent } from '../edit-task-dialog/edit-task-dialog.component';

@Component({
  selector: 'app-todo',
  standalone: true,
  imports: [
    CommonModule,
    TableModule,
    DragDropModule,
    ButtonModule
  ],
  templateUrl: './todo.component.html',
  styleUrl: './todo.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class TodoComponent {
  private readonly confirmationService = inject(ConfirmationService);
  private readonly taskTemplateExtendedService = inject(TaskTemplateExtendedService);
  private readonly dialogService = inject(DialogService);
  private readonly translate = inject(TranslateService);

  @Input() items$!: Observable<any[]>;
  @Input() weekDayDate!: string;
  @Input() cols!: any[];

  isDragOver = false;
  newIndex: number | null = null;
  originalIndex!: number;

  onDragStart(event: DragEvent, rowData: any, index: number) {
    // Convert the rowData object to a JSON string
    const data = JSON.stringify(rowData);

    // Use the dataTransfer.setData() method to set the data to be transferred
    // "application/json" is used as a type identifier to signify the type of data being transferred
    event.dataTransfer?.setData(APPLICATION_JSON, data);

    this.originalIndex = index;
  }

  onDragOver(event: DragEvent) {
    event.preventDefault(); //just in case ako neki browser ne dopušta

    //isDragOver je inicijalno false da tablica nema obrub
    //kada počnem radit drag, poprimi true, ali kad kursor izađe iz droppable zone
    //makne se 'p-draggable-enter' klasa sama zbog PrimeNG-a.
    //ali property ostane true. (neće se odma syncat isDragOver i [ngClass])
    //na onDrop, postavi se na false, što onda makne klasu.
    //moram ovdje postavit true, jer ako je inicijalno false
    //a onDrop postavi na false, change detection neće registrirat
    //promjenu
    //onDragLeave radi konflike sa onDragOver ako je drag zone i drop zone isti
    //kao u mom slučaju

    this.isDragOver = true;
  }

  onDrop(event: DragEvent) {
    event.preventDefault(); //just in case ako neki browser ne dopušta
    //moramo ovdje stavit false inače bi ostao border
    this.isDragOver = false;

    const data = event.dataTransfer?.getData(APPLICATION_JSON);
    const rowData = JSON.parse(data!);

    //null je kad dropam task, ali nije promijenio poziciju ili ga pomičem na drugi dan#
    if (this.newIndex !== null && this.newIndex !== this.originalIndex) {
      this.taskTemplateExtendedService.reorderTaskOccurrence(rowData.id, rowData.committedDate, this.newIndex);
    }

    //logika za commitanje taska na neki drugi dan#
    //sad provjera ako task želim prebacit na neki drugi dan, onda zovi backend#
    if (rowData.committedDate !== this.weekDayDate) {
      this.taskTemplateExtendedService.commitTaskOccurrence(rowData.id, this.weekDayDate);
    }

    this.newIndex = null;
  }

  onRowReorder(event: any) {
    this.newIndex = event.dropIndex;
  }

  generateCaption(weekDayDate: string): string {
    const dueDate = new Date(weekDayDate);
    dueDate.setHours(0, 0, 0, 0); // Set to the start of the day, timezone is UTC

    const today = new Date();
    today.setHours(0, 0, 0, 0);

    const optionsWeekday: Intl.DateTimeFormatOptions = { weekday: 'long' };
    const formattedWeekday = this.translate.instant(dueDate.toLocaleDateString('en-US', optionsWeekday));

    // Manually format the date to avoid extra spaces and exclude the year
    const day = ('0' + dueDate.getDate()).slice(-2);
    const month = ('0' + (dueDate.getMonth() + 1)).slice(-2);
    const formattedDate = `${day}.${month}.`;

    if (dueDate.getTime() === today.getTime()) {
      return `${formattedWeekday}, ${formattedDate} (${this.translate.instant('today')})`;
    } else {
      return `${formattedWeekday}, ${formattedDate}`;
    }
  }

  completeTaskOccurrence(taskOccurrence: TaskOccurrenceDto) {
    this.taskTemplateExtendedService.completeTaskOccurrence(taskOccurrence.id!);
  }

  editTaskOccurrence(taskOccurrence: TaskOccurrenceDto) {
    this.dialogService.open(EditTaskDialogComponent, {
      data: {
        taskOccurrence: taskOccurrence
      }
    });
  }

  returnTaskOccurrenceToGroup(taskOccurrence: TaskOccurrenceDto) {
    this.taskTemplateExtendedService.commitTaskOccurrence(taskOccurrence.id!, null);
  }

  deleteTaskTemplate(taskOccurrence: TaskOccurrenceDto) {
    this.confirmationService.confirm({
      header: this.translate.instant('deleteConfirmation'),
      acceptLabel: this.translate.instant('confirm'),
      rejectLabel: this.translate.instant('cancel'),
      accept: () => {
        this.taskTemplateExtendedService.deleteTaskTemplate(taskOccurrence.taskTemplate!.id!);
      }
    });
  }
}
