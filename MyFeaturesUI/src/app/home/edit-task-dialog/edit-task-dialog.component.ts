import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, OnDestroy, OnInit, inject } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { TranslateModule, TranslateService } from '@ngx-translate/core';
import { CalendarModule } from 'primeng/calendar';
import { DialogModule } from 'primeng/dialog';
import { DynamicDialogConfig, DynamicDialogRef } from 'primeng/dynamicdialog';
import { InputNumberModule } from 'primeng/inputnumber';
import { InputTextModule } from 'primeng/inputtext';
import { RadioButtonModule } from 'primeng/radiobutton';
import { SelectButtonModule } from 'primeng/selectbutton';
import { Subject, combineLatest, take, takeUntil } from 'rxjs';
import { IntervalType, TaskTemplateService, TaskOccurrenceDto } from '../../../infrastructure';
import { TaskTemplateExtendedService } from '../../extended-services/task-template-extended-service';

@Component({
  selector: 'app-edit-task-dialog',
  standalone: true,
  imports: [
    CommonModule,
    DialogModule,
    CalendarModule,
    InputNumberModule,
    ReactiveFormsModule,
    SelectButtonModule,
    InputTextModule,
    FormsModule,
    RadioButtonModule,
    TranslateModule
  ],
  templateUrl: './edit-task-dialog.component.html',
  styleUrl: './edit-task-dialog.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush
})
export class EditTaskDialogComponent implements OnInit, OnDestroy {
  private readonly destroy$ = new Subject<void>();

  form!: FormGroup;
  private readonly formBuilder = inject(FormBuilder);
  private readonly ref = inject(DynamicDialogRef);
  private readonly config = inject(DynamicDialogConfig);
  private readonly taskTemplateService = inject(TaskTemplateService);
  private readonly taskTemplateExtendedService = inject(TaskTemplateExtendedService);
  private readonly translate = inject(TranslateService);

  taskOccurrence: TaskOccurrenceDto = {}; //trenutno selektiran
  stateOptions: any[] = [
    { label: this.translate.instant('editTask.oneTime'), value: false },
    { label: this.translate.instant('editTask.recurring'), value: true }
  ];

  renewOptions: any[] = [
    { label: this.translate.instant('editTask.onDueDate'), value: true },
    { label: this.translate.instant('editTask.onCompletionDate'), value: false }
  ];
  ingredient!: string;

  intervalType = IntervalType;

  ngOnInit() {
    this.form = this.formBuilder.group({
      description: ['', Validators.required],
      recurring: [false, Validators.required],
      dueDate: [null],
      renewOnDueDate: [null],
      intervalType: [null],
      intervalValue: [null]
    });

    //ako je edit, povuci s backenda i prikaži na formi
    if (this.config.data?.taskOccurrence) {
      this.loadTaskOccurrence(this.config.data.taskOccurrence);

      //nema mijenjanja recurringa na edit
      this.form.get('recurring')?.disable();
    }
    else {
      //inače za create disable-a by default
      this.disableNonRecurringFields();
    }

    this.setupValueChangeHandlers();
  }

  private disableNonRecurringFields() {
    this.form.get('renewOnDueDate')?.disable();
    this.form.get('intervalType')?.disable();
    this.form.get('intervalValue')?.disable();
  }

  private setupValueChangeHandlers() {
    combineLatest([
      this.form.get('recurring')!.valueChanges,
      this.form.get('dueDate')!.valueChanges,
    ])
      .pipe(takeUntil(this.destroy$))
      .subscribe(([recurring, dueDate]) => {
        if (recurring && dueDate) {
          this.form.get('renewOnDueDate')?.enable();
        } else {
          this.form.get('renewOnDueDate')?.disable();
          this.form.get('renewOnDueDate')?.reset();
        }
      });

    this.form.get('renewOnDueDate')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((renewOnDueDate) => {
        if (renewOnDueDate === null) {
          this.form.get('intervalType')?.disable();
          this.form.get('intervalType')?.reset();
        } else {
          this.form.get('intervalType')?.enable();
          //ako je odabrao datum i recurring je, mora odabrat tip sekvence
          this.form.get('intervalType')?.addValidators(Validators.required);
        }

        this.form.get('intervalType')?.updateValueAndValidity();
      });

    this.form.get('intervalType')!.valueChanges
      .pipe(takeUntil(this.destroy$))
      .subscribe((intervalType) => {
        if (intervalType) {
          this.form.get('intervalValue')?.enable();
          this.form.get('intervalValue')?.addValidators(Validators.required);
        } else {
          //validator automatski maknut
          this.form.get('intervalValue')?.disable();
          this.form.get('intervalValue')?.reset();
        }

        this.form.get('intervalValue')?.updateValueAndValidity();
      });
  }

  completeTaskOccurrence(taskOccurrence: TaskOccurrenceDto) {
    this.taskTemplateExtendedService.completeTaskOccurrence(taskOccurrence.id!);
  }

  loadTaskOccurrence(taskOccurrence: TaskOccurrenceDto) {
    this.taskTemplateService.getTaskOccurrenceById(taskOccurrence.id!)
      .pipe(take(1))
      .subscribe((loadedTaskOccurrence) => {
        this.displayTaskOccurrence(loadedTaskOccurrence);
      });
  }

  displayTaskOccurrence(taskOccurrence: TaskOccurrenceDto): void {
    this.taskOccurrence = taskOccurrence;

    const description = taskOccurrence.committedDate ? taskOccurrence.description : taskOccurrence.taskTemplate!.description;

    this.form.patchValue({
      description: description,
      recurring: taskOccurrence.taskTemplate!.recurring,
      renewOnDueDate: taskOccurrence.taskTemplate!.renewOnDueDate,
      dueDate: taskOccurrence.dueDate ? new Date(taskOccurrence.dueDate) : null,
      intervalValue: taskOccurrence.taskTemplate!.intervalValue,
      intervalType: taskOccurrence.taskTemplate!.intervalType
    });
  }

  saveTaskOccurrence() {
    if (this.form.dirty) {
      let taskOccurrence: TaskOccurrenceDto;

      if (this.taskOccurrence.id) {
        taskOccurrence = {
          ...this.taskOccurrence,
          dueDate: this.form.getRawValue().dueDate ? this.form.getRawValue().dueDate.toLocaleDateString('en-CA') : null,
          taskTemplate: {
            ...this.taskOccurrence.taskTemplate,
            renewOnDueDate: this.form.getRawValue().renewOnDueDate,
            intervalType: this.form.getRawValue().intervalType,
            intervalValue: this.form.getRawValue().intervalValue
          }
        };

        this.updateDescriptions(taskOccurrence);

        this.taskTemplateExtendedService.updateTaskOccurrence(taskOccurrence);
      } else {
        taskOccurrence = {
          description: this.form.getRawValue().description,
          //spremam samo datum bez vremenske komponente (gledam datum iz kalendara, vremenska zona nije važna) ".toLocale(en-CA)"
          dueDate: this.form.getRawValue().dueDate ? this.form.getRawValue().dueDate.toLocaleDateString('en-CA') : null,
          taskTemplate: {
            ...this.form.getRawValue()
          }
        };

        this.taskTemplateExtendedService.createTaskOccurrence(taskOccurrence)
      }
    }

    this.hideDialog();
  }

  private updateDescriptions(taskOccurrence: TaskOccurrenceDto) {
    const description = this.form.getRawValue().description;

    //za one time, update se radi na oba descriptiona
    if (!taskOccurrence.taskTemplate!.recurring) {
      taskOccurrence.taskTemplate!.description = description;
      taskOccurrence.description = description;
    }
    //ako je update original task occurrence-a (ne iz weekdays tablice), onda samo njega update-at iz forme
    else if (taskOccurrence.committedDate) {
      taskOccurrence.description = description;
    } else {
      //inače update-at child task occurrence iz weekdays
      taskOccurrence.taskTemplate!.description = description;
    }
  }

  hideDialog(): void {
    this.ref.close();
  }

  ngOnDestroy() {
    this.destroy$.next();
    this.destroy$.complete();
  }
}
