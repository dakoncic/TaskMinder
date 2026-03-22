import { Injectable, inject } from '@angular/core';
import { BehaviorSubject, switchMap, take } from 'rxjs';
import { CommitTaskOccurrenceDto, TaskOccurrenceDto, TaskTemplateService, UpdateTaskOccurrenceIndexDto, UpdateTaskTemplateIndexDto } from '../../infrastructure';

@Injectable({
  providedIn: 'root'
})
export class TaskTemplateExtendedService {
  private readonly taskTemplateService = inject(TaskTemplateService);

  private readonly oneTimeTaskTemplatesSourceSubject = new BehaviorSubject<void>(undefined);
  private readonly recurringTaskTemplatesSourceSubject = new BehaviorSubject<void>(undefined);
  private readonly weekDaysSourceSubject = new BehaviorSubject<void>(undefined);

  oneTimeItems$ = this.oneTimeTaskTemplatesSourceSubject.pipe(
    switchMap(() => this.taskTemplateService.getOneTimeTaskOccurrences(this.getCurrentLocalDate()))
  );

  recurringItems$ = this.recurringTaskTemplatesSourceSubject.pipe(
    switchMap(() => this.taskTemplateService.getRecurringTaskOccurrences(this.getCurrentLocalDate()))
  );

  weekData$ = this.weekDaysSourceSubject.pipe(
    switchMap(() => this.taskTemplateService.getCommittedTaskOccurrencesForNextWeek(this.getCurrentLocalDate()))
  );

  createTaskOccurrence(taskOccurrence: TaskOccurrenceDto) {
    return this.taskTemplateService.createTaskTemplateAndOccurrence(taskOccurrence).pipe(
      take(1),
    )
      .subscribe(() => {
        this.refreshAllTaskLists();
      });
  }

  updateTaskOccurrence(taskOccurrence: TaskOccurrenceDto) {
    if (taskOccurrence.id === undefined) {
      return;
    }

    return this.taskTemplateService.updateTaskTemplateAndOccurrence(taskOccurrence.id, taskOccurrence).pipe(
      take(1),
    )
      .subscribe(() => {
        this.refreshAllTaskLists();
      });
  }

  deleteTaskTemplate(taskTemplateId: number) {
    return this.taskTemplateService.deleteTaskTemplateAndOccurrences(taskTemplateId).pipe(
      take(1),
    )
      .subscribe(() => {
        this.refreshAllTaskLists();
      });
  }

  completeTaskOccurrence(taskOccurrenceId: number) {
    return this.taskTemplateService.completeTaskOccurrence(taskOccurrenceId, this.getCurrentLocalDate()).pipe(
      take(1),
    )
      .subscribe(() => {
        this.refreshAllTaskLists();
      });
  }

  commitTaskOccurrence(taskOccurrenceId: number, commitDay: string | null) {
    const commitTaskOccurrenceDto: CommitTaskOccurrenceDto = {
      commitDay,
      taskOccurrenceId,
    };

    return this.taskTemplateService.commitTaskOccurrence(commitTaskOccurrenceDto).pipe(
      take(1),
    )
      .subscribe(() => {
        this.refreshAllTaskLists();
      });
  }

  reorderTaskTemplate(taskTemplateId: number, newIndex: number, recurring: boolean) {
    const updatedTaskTemplateIndex: UpdateTaskTemplateIndexDto = {
      taskTemplateId,
      newIndex,
      recurring,
    };

    this.taskTemplateService.reorderTaskTemplateInsideGroup(updatedTaskTemplateIndex).pipe(
      take(1),
    )
      .subscribe(() => {
        if (recurring) {
          this.recurringTaskTemplatesSourceSubject.next();
        } else {
          this.oneTimeTaskTemplatesSourceSubject.next();
        }
      });
  }

  reorderTaskOccurrence(taskOccurrenceId: number, commitDay: string, newIndex: number) {
    const updatedTaskOccurrenceIndex: UpdateTaskOccurrenceIndexDto = {
      commitDay,
      taskOccurrenceId,
      newIndex
    };

    this.taskTemplateService.reorderTaskOccurrenceInsideGroup(updatedTaskOccurrenceIndex).pipe(
      take(1),
    )
      .subscribe(() => {
        this.weekDaysSourceSubject.next();
      });
  }

  private refreshAllTaskLists() {
    this.oneTimeTaskTemplatesSourceSubject.next();
    this.recurringTaskTemplatesSourceSubject.next();
    this.weekDaysSourceSubject.next();
  }

  private getCurrentLocalDate() {
    const now = new Date();
    const year = now.getFullYear();
    const month = `${now.getMonth() + 1}`.padStart(2, '0');
    const day = `${now.getDate()}`.padStart(2, '0');

    return `${year}-${month}-${day}`;
  }
}
