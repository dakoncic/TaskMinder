using Core.DomainModels;
using Core.Helpers;
using Core.Interfaces;
using Infrastructure.DAL;
using Infrastructure.Interfaces.IRepository;
using Mapster;
using Entity = Infrastructure.Entities;


namespace Core.Services
{
    public class NotepadService : BaseService, INotepadService
    {
        private readonly MyFeaturesDbContext _context;
        private readonly IGenericRepository<Entity.Notepad> _notepadRepository;

        public NotepadService(
            MyFeaturesDbContext context,
            IGenericRepository<Entity.Notepad> notepadRepository
            )
        {
            _context = context;
            _notepadRepository = notepadRepository;
        }

        public async Task Create()
        {
            var notepadEntity = new Entity.Notepad();

            var maxRowIndexNotepadEntity = await _notepadRepository.GetFirstOrDefaultAsync(
                //ne želi pustit OrderByDescending ako nemam trivijalni "true" filter
                x => true,
                q => q.OrderByDescending(x => x.RowIndex)
            );

            int startIndex = maxRowIndexNotepadEntity != null ? maxRowIndexNotepadEntity.RowIndex!.Value + 1 : 1;

            notepadEntity.RowIndex = startIndex;

            _notepadRepository.Add(notepadEntity);

            await _context.SaveChangesAsync();
        }

        //ovo refaktorat da radim single item fetch ipak kao i svagdje drugdje
        public async Task Update(int notepadId, Notepad notepadDomain)
        {
            var notepadEntity = await _notepadRepository.GetByIdAsync(notepadId);

            CheckIfNull(notepadEntity, $"Notepad with ID {notepadId} not found.");

            int currentIndex = notepadEntity.RowIndex!.Value;
            int newIndex = notepadDomain.RowIndex!.Value;

            notepadDomain.Adapt(notepadEntity);

            var notepadsEntity = await _notepadRepository.GetAllAsync(
                orderBy: x => x.OrderBy(n => n.RowIndex));

            if (newIndex < 1 || newIndex > notepadsEntity.Count())
            {
                throw new ArgumentOutOfRangeException(nameof(notepadDomain), "Index out of range.");
            }

            if (newIndex != currentIndex)
            {
                var itemsToUpdateEntity = notepadsEntity.Where(n => n.Id != notepadId).ToList();

                RowIndexHelper.ManaulReorderRowIndexes<Entity.Notepad>(itemsToUpdateEntity, newIndex, currentIndex);

                notepadEntity.RowIndex = newIndex;
            }

            await _context.SaveChangesAsync();
        }

        public async Task Delete(int notepadId)
        {
            var notepadEntity = await _notepadRepository.GetByIdAsync(notepadId);

            CheckIfNull(notepadEntity, $"Notepad with ID {notepadId} not found.");

            int deletedIndex = notepadEntity.RowIndex!.Value;

            await _notepadRepository.UpdateBatchAsync(
                x => x.RowIndex > deletedIndex,
                x => new Entity.Notepad { RowIndex = x.RowIndex - 1 }
            );

            _notepadRepository.Delete(notepadId);
            await _context.SaveChangesAsync();
        }

        public async Task<List<Notepad>> GetAll()
        {
            var notepadsEntity = await _notepadRepository.GetAllAsync(
                orderBy: x => x.OrderBy(n => n.RowIndex)
            );

            return notepadsEntity.Adapt<List<Notepad>>();
        }
    }
}
