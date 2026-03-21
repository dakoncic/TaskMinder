export * from './notepad.service';
import { NotepadService } from './notepad.service';
export * from './taskTemplate.service';
import { TaskTemplateService } from './taskTemplate.service';
export const APIS = [NotepadService, TaskTemplateService];
