import { CommonModule } from '@angular/common';
import { Component, EventEmitter, inject, Input, Output } from '@angular/core';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ConfirmationService } from 'primeng/api';
import { ButtonModule } from 'primeng/button';
import { DialogService } from 'primeng/dynamicdialog';
import { TableModule } from 'primeng/table';
import { TaskOccurrenceDto, TaskTemplateDto } from '../../../infrastructure';
import { TaskTemplateExtendedService } from '../../extended-services/task-template-extended-service';
import { APPLICATION_JSON } from '../../shared/constants';
import { EditTaskDialogComponent } from '../edit-task-dialog/edit-task-dialog.component';

@Component({
  selector: 'app-active-task-occurrences-table',
  standalone: true,
  imports: [
    CommonModule,
    TableModule,
    ButtonModule,
    TranslateModule
  ],
  templateUrl: './active-task-occurrences-table.component.html',
  styleUrl: './active-task-occurrences-table.component.scss'
})
export class ActiveTaskOccurrencesTableComponent {
  @Input() items!: TaskTemplateDto[];
  @Input() columns!: any[];
  @Input() currentDay!: string | null;
  @Input() title!: string;
  @Input() isRecurring!: boolean;
  @Output() currentDayChange = new EventEmitter<any>();

  private readonly confirmationService = inject(ConfirmationService);
  private readonly taskTemplateExtendedService = inject(TaskTemplateExtendedService);
  private readonly dialogService = inject(DialogService);
  private readonly translate = inject(TranslateService);

  newIndex: number | null = null;
  originalIndex!: number;

  onRowReorder(event: any) {
    this.newIndex = event.dropIndex;
  }

  onDragStart(event: DragEvent, rowData: any, index: number) {
    // Convert the rowData object to a JSON string
    const data = JSON.stringify(rowData);

    // Use the dataTransfer.setData() method to set the data to be transferred
    // "application/json" is used as a type identifier to signify the type of data being transferred
    event.dataTransfer?.setData(APPLICATION_JSON, data);

    this.originalIndex = index;
  }

  onDrop(event: DragEvent, recurring: boolean) {
    event.preventDefault(); //just in case ako neki browser ne dopušta

    const data = event.dataTransfer?.getData(APPLICATION_JSON);
    const rowData = JSON.parse(data!);

    //null je kad dropam task, ali nije promijenio poziciju ili ga pomičem na drugi dan#
    if (this.newIndex !== null && this.newIndex !== this.originalIndex) {
      this.taskTemplateExtendedService.reorderTaskTemplate(rowData.taskTemplate.id, this.newIndex, recurring);
    }

    this.newIndex = null;
  }

  assignTaskOccurrenceToSelectedWeekday(taskOccurrence: TaskOccurrenceDto) {
    if (this.currentDay) {
      this.taskTemplateExtendedService.commitTaskOccurrence(taskOccurrence.id!, this.currentDay);
    }
  }

  editTaskOccurrence(taskOccurrence: TaskOccurrenceDto) {
    this.resetCurrentDay();

    this.dialogService.open(EditTaskDialogComponent, {
      data: {
        taskOccurrence: taskOccurrence
      }
    });
  }

  deleteTaskTemplate(taskOccurrence: TaskOccurrenceDto) {
    this.resetCurrentDay();

    this.confirmationService.confirm({
      header: this.translate.instant('deleteConfirmation'),
      acceptLabel: this.translate.instant('confirm'),
      rejectLabel: this.translate.instant('cancel'),
      accept: () => {
        this.taskTemplateExtendedService.deleteTaskTemplate(taskOccurrence.taskTemplate?.id!);
      }
    });

    //ovo je primjer ako svu logiku radim kroz samo ovu komponentu za delete i get all
    //switchMap će biti unsubscribe-an kada i njegov parent
    //items$ budu unsubscribe-ani, a bit će zbog async pipe-a u html-u
    // ide kroz extended servis, ne lokalno
    // this.items$ = this.taskTemplateService.deleteTaskTemplateAndOccurrences(this.taskOccurrence.taskTemplate.id!)
    //   .pipe(
    //     switchMap(() => this.taskTemplateService.getOneTimeTaskOccurrences())
    //   );
  }

  completeTaskOccurrence(taskOccurrence: TaskOccurrenceDto) {
    this.resetCurrentDay();

    this.taskTemplateExtendedService.completeTaskOccurrence(taskOccurrence.id!);
  }

  private resetCurrentDay() {
    this.currentDayChange.emit(null);
  }
}
