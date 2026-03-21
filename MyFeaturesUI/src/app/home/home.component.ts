import { CommonModule, DatePipe } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnInit, inject } from '@angular/core';
import { FormsModule } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { ButtonModule } from 'primeng/button';
import { DividerModule } from 'primeng/divider';
import { DragDropModule } from 'primeng/dragdrop';
import { DialogService } from 'primeng/dynamicdialog';
import { InputTextModule } from 'primeng/inputtext';
import { SelectButtonModule } from 'primeng/selectbutton';
import { ToolbarModule } from 'primeng/toolbar';
import { map, of, take } from 'rxjs';
import { TaskTemplateExtendedService } from '../extended-services/task-template-extended-service';
import { NotepadExtendedService } from '../extended-services/notepad-extended-service';
import { ActiveTaskOccurrencesTableComponent } from './active-task-occurrences-table/active-task-occurrences-table.component';
import { EditTaskDialogComponent } from './edit-task-dialog/edit-task-dialog.component';
import { NotepadComponent } from './notepad/notepad.component';
import { TodoComponent } from './todo/todo.component';

@Component({
  selector: 'app-home',
  standalone: true,
  imports: [
    CommonModule,
    FormsModule,
    ButtonModule,
    ToolbarModule,
    InputTextModule,
    SelectButtonModule,
    TodoComponent,
    ActiveTaskOccurrencesTableComponent,
    NotepadComponent,
    DividerModule,
    DragDropModule,
    TranslateModule
  ],
  providers: [
    //moram provide-at zbog *null injector error-a*
    DialogService,
    DatePipe
  ],
  templateUrl: './home.component.html',
  styleUrl: './home.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class HomeComponent implements OnInit {
  private readonly taskTemplateExtendedService = inject(TaskTemplateExtendedService);
  private readonly notepadExtendedService = inject(NotepadExtendedService);
  private readonly dialogService = inject(DialogService);
  private readonly datePipe = inject(DatePipe);
  private readonly translate = inject(TranslateService);

  newIndex: number | null = null;
  originalIndex!: number;

  cols: any[] = [];
  currentDay: string | null = null;

  weekdays: any[] = [];

  oneTimeItems$ = this.taskTemplateExtendedService.oneTimeItems$.pipe(
    map(oneTimeTaskOccurrences => oneTimeTaskOccurrences.map(taskOccurrence => ({
      ...taskOccurrence,
      description: taskOccurrence.taskTemplate?.description,
      dueDate: taskOccurrence.dueDate ? this.datePipe.transform(taskOccurrence.dueDate, 'dd.MM.yy') : null
    })))
  );

  recurringItems$ = this.taskTemplateExtendedService.recurringItems$.pipe(
    map(recurringTaskOccurrences => recurringTaskOccurrences.map(taskOccurrence => ({
      ...taskOccurrence,
      description: taskOccurrence.taskTemplate?.description,
      dueDate: taskOccurrence.dueDate ? this.datePipe.transform(taskOccurrence.dueDate, 'dd.MM.yy') : null
    })))
  );

  weekData$ = this.taskTemplateExtendedService.weekData$.pipe(
    map(weekdata => weekdata.map(daydata => ({
      weekDayDate: daydata.weekDayDate!,
      items$: of(daydata.taskOccurrences!).pipe(
        map(taskOccurrences => taskOccurrences.map(taskOccurrence => ({
          ...taskOccurrence,
          originalDescription: taskOccurrence.taskTemplate?.description,
          dueDate: taskOccurrence.dueDate ? this.datePipe.transform(taskOccurrence.dueDate, 'dd.MM.yy') : null
        })))
      )
    })))
  );

  notepads$ = this.notepadExtendedService.notepads$;

  ngOnInit() {
    this.initializeWeekdays();

    this.cols = [
      { field: 'description' },
      { field: 'dueDate', align: 'right' }
    ];
  }

  initializeWeekdays(): void {
    const addDays = (date: Date, days: number): Date => {
      let result = new Date(date);
      result.setDate(result.getDate() + days);
      return result;
    };

    this.weekData$
      .pipe(take(1))
      .subscribe(weekData => {
        const updates = [];
        for (let i = 0; i < weekData.length; i++) {
          let dateToAdd = addDays(new Date(), i);

          let dayNameInEnglish = new Intl.DateTimeFormat('en-US', { weekday: 'long' }).format(dateToAdd);

          let dayName = i === 0
            ? this.translate.instant(dayNameInEnglish) + ' (' + this.translate.instant('today') + ')'
            : this.translate.instant(dayNameInEnglish);

          let localDateStr = dateToAdd.toLocaleDateString('en-CA', {
            year: 'numeric', month: '2-digit', day: '2-digit'
          });

          updates.push({
            name: dayName,
            value: localDateStr
          });
        }

        // samo push modificira array ali se ne mijenja referenca što ne okine change detection
        // moram koristit spread syntax da napravi novu referencu
        this.weekdays = [...updates];
      });
  }

  openNew() {
    this.dialogService.open(EditTaskDialogComponent, {});
  }

  createNewNotepad() {
    this.notepadExtendedService.createNotepad();
  }
}
