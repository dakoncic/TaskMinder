using Core.DomainModels;
using Core.Helpers;
using Core.Interfaces;
using Infrastructure.DAL;
using Infrastructure.Interfaces.IRepository;
using Mapster;
using Shared;
using System.Linq.Expressions;
//using Infrastructure.Entities; ako imam error ambiguous reference, onda maknut ovu liniju
using Entity = Infrastructure.Entities;

namespace Core.Services
{
    public class ItemService : BaseService, IItemService
    {
        private readonly MyFeaturesDbContext _context;
        private readonly IGenericRepository<Entity.Item, int> _itemRepository;
        private readonly IGenericRepository<Entity.ItemTask, int> _itemTaskRepository;

        public ItemService(
            MyFeaturesDbContext context,
            IGenericRepository<Entity.Item, int> itemRepository,
            IGenericRepository<Entity.ItemTask, int> itemTaskRepository
            )
        {
            _context = context;
            _itemRepository = itemRepository;
            _itemTaskRepository = itemTaskRepository;
        }

        public async Task CreateItemAndTask(ItemTask itemTaskDomain)
        {
            if (itemTaskDomain.DueDate != null)
            {
                itemTaskDomain.CommittedDate = itemTaskDomain.DueDate;
                itemTaskDomain.RowIndex = await GetNewItemTaskRowIndex(itemTaskDomain.CommittedDate);
            }
            else
            {
                //ako je DueDate postavljen na null, onda daj index parentu
                itemTaskDomain.Item.RowIndex = await GetNewItemRowIndex(itemTaskDomain.Item.Recurring);
            }

            var itemEntity = itemTaskDomain.Item.Adapt<Entity.Item>();
            var itemTaskEntity = itemTaskDomain.Adapt<Entity.ItemTask>();

            itemEntity.ItemTasks.Add(itemTaskEntity);

            _itemRepository.Add(itemEntity);

            await _context.SaveChangesAsync();
        }

        public async Task<ItemTask> GetItemTaskById(int itemTaskId)
        {
            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId, "Item");

            //ostavljamo check ovdje u servisu, ako je entity null, onda on ne može zvat
            //nikakvu metodu da provjeri sam sebe jeli null
            CheckIfNull(itemTaskEntity, $"ItemTask with ID {itemTaskId} not found.");

            return itemTaskEntity.Adapt<ItemTask>();
        }

        public async Task UpdateItemAndTask(int itemTaskId, ItemTask itemTaskDomain)
        {
            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId, "Item");

            CheckIfNull(itemTaskEntity, $"ItemTask with ID {itemTaskId} not found.");

            var updatedItemTask = itemTaskEntity.Adapt<ItemTask>();
            itemTaskDomain.Adapt(updatedItemTask);

            var oldDueDate = itemTaskEntity.DueDate;
            var newDueDate = updatedItemTask.DueDate?.Date;
            var oldCommittedDate = itemTaskEntity.CommittedDate;

            if (oldDueDate?.Date != newDueDate?.Date)
            {
                await HandleDueDateChange(itemTaskEntity, updatedItemTask, oldCommittedDate, newDueDate);
            }

            updatedItemTask.Adapt(itemTaskEntity);
            await _context.SaveChangesAsync();
        }

        private async Task HandleDueDateChange(Entity.ItemTask itemTaskEntity, ItemTask updatedItemTask, DateTime? oldCommittedDate, DateTime? newDueDate)
        {
            updatedItemTask.CommittedDate = newDueDate?.Date;
            await UpdateItemTaskRowIndexesIfDateProvided(oldCommittedDate, itemTaskEntity.RowIndex);

            if (newDueDate is not null)
            {
                updatedItemTask.RowIndex = await GetNewItemTaskRowIndex(updatedItemTask.CommittedDate);
                //ako je postavljen dueDate, reset RowIndex
                updatedItemTask.Item.RowIndex = null;
            }
            else
            {
                //ako je DueDate postavljen na null, onda postavi novi index na Item
                updatedItemTask.Item.RowIndex = await GetNewItemRowIndex(updatedItemTask.Item.Recurring);
            }
        }

        public async Task DeleteItemAndTasks(int itemId)
        {
            var itemEntity = await _itemRepository.GetByIdAsync(itemId, "ItemTasks");

            CheckIfNull(itemEntity, $"Item with ID {itemId} not found.");

            _itemRepository.Delete(itemId);

            await UpdateRowIndexesForRemainingItems(itemEntity);

            //dohvaćam committan ItemTask za Item ako postoji
            var itemTaskEntity = itemEntity.ItemTasks.FirstOrDefault(x => x.CommittedDate != null && x.CompletionDate == null);

            //i ako je bio, za sve itemTaskove na taj dan im pomičem index
            if (itemTaskEntity != null)
            {
                await UpdateItemTaskRowIndexesIfDateProvided(itemTaskEntity.CommittedDate, itemTaskEntity.RowIndex);
            }

            await _context.SaveChangesAsync();
        }

        public async Task CompleteItemTask(int itemTaskId)
        {
            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId, "Item");

            CheckIfNull(itemTaskEntity, $"ItemTask with ID {itemTaskId} not found.");

            itemTaskEntity.CompletionDate = DateTime.Now;

            await UpdateItemTaskRowIndexesIfDateProvided(itemTaskEntity.CommittedDate, itemTaskEntity.RowIndex);

            if (!itemTaskEntity.Item.Recurring)
            {
                await UpdateRowIndexesForRemainingItems(itemTaskEntity.Item);

                itemTaskEntity.Item.Completed = true;
            }
            else
            {
                //ako je novi dueDate null, tj. nije poprimio novi datum, onda Item mora dobiti RowIndex
                if (itemTaskEntity.DueDate is null)
                {
                    itemTaskEntity.Item.RowIndex = await GetNewItemRowIndex(itemTaskEntity.Item.Recurring);
                }
                //al ako nije null, onda RowIndex ostaje default null

                var itemTaskDomain = itemTaskEntity.Adapt<ItemTask>();

                //dobar primjer enkapsulacije biznis logike u domain klasu
                var newItemTask = itemTaskDomain.CreateNewRecurringTask();
                if (newItemTask.CommittedDate != null)
                {
                    newItemTask.RowIndex = await GetNewItemTaskRowIndex(newItemTask.CommittedDate);
                }

                var newItemTaskEntity = newItemTask.Adapt<Entity.ItemTask>();
                newItemTaskEntity.Description = itemTaskEntity.Item.Description;
                _itemTaskRepository.Add(newItemTaskEntity);
            }

            await _context.SaveChangesAsync();
        }
        public async Task CommitItemTaskOrReturnToGroup(DateTime? newCommitDay, int itemTaskId)
        {
            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemTaskId, "Item");
            CheckIfNull(itemTaskEntity, $"ItemTask with ID {itemTaskId} not found.");

            var itemTaskDomain = itemTaskEntity.Adapt<ItemTask>();

            var oldCommittedDate = itemTaskDomain.CommittedDate;

            if (oldCommittedDate?.Date != newCommitDay?.Date)
            {
                itemTaskDomain.CommittedDate = newCommitDay?.Date;

                await UpdateItemTaskRowIndexesIfDateProvided(oldCommittedDate, itemTaskEntity.RowIndex);

                if (newCommitDay is not null)
                {
                    if (itemTaskEntity.Item.RowIndex.HasValue)
                    {
                        await UpdateRowIndexesForRemainingItems(itemTaskEntity.Item);
                    }
                    itemTaskDomain.RowIndex = await GetNewItemTaskRowIndex(newCommitDay);
                    itemTaskDomain.Item.RowIndex = null;
                }
                else
                {
                    itemTaskDomain.Description = itemTaskDomain.Item.Description;
                    itemTaskDomain.DueDate = null;
                    itemTaskDomain.RowIndex = null;
                    itemTaskDomain.Item.RowIndex = await GetNewItemRowIndex(itemTaskDomain.Item.Recurring);
                }
            }

            itemTaskDomain.Adapt(itemTaskEntity);

            await _context.SaveChangesAsync();
        }

        public async Task ReorderItemInsideGroup(int itemId, int newIndex, bool recurring)
        {
            var itemEntity = await _itemRepository.GetByIdAsync(itemId);

            CheckIfNull(itemEntity, $"Item with ID {itemId} not found.");

            int currentIndex = itemEntity.RowIndex!.Value;

            Expression<Func<Entity.Item, bool>> filter = x =>
                !x.Completed &&
                x.Recurring.Equals(recurring) &&
                x.Id != itemId;
            Func<IQueryable<Entity.Item>, IOrderedQueryable<Entity.Item>> orderBy = q => q.OrderBy(x => x.RowIndex);

            var itemsEntity = await _itemRepository.GetAllAsync(filter, orderBy);

            RowIndexHelper.ManaulReorderRowIndexes<Entity.Item>(itemsEntity, newIndex, currentIndex);

            itemEntity.RowIndex = newIndex;

            await _context.SaveChangesAsync();
        }

        public async Task ReorderItemTaskInsideGroup(int itemId, DateTime commitDate, int newIndex)
        {
            var itemTaskEntity = await _itemTaskRepository.GetByIdAsync(itemId);

            CheckIfNull(itemTaskEntity, $"ItemTask with ID {itemId} not found.");

            int currentIndex = itemTaskEntity.RowIndex!.Value;

            Expression<Func<Entity.ItemTask, bool>> filter =
                x =>
                x.CompletionDate == null &&
                x.CommittedDate.HasValue &&
                x.CommittedDate.Value.Date == commitDate.Date &&
                x.Id != itemId;
            Func<IQueryable<Entity.ItemTask>, IOrderedQueryable<Entity.ItemTask>> orderBy = q => q.OrderBy(x => x.RowIndex);

            var itemTasksForDateEntity = await _itemTaskRepository.GetAllAsync(filter, orderBy);

            RowIndexHelper.ManaulReorderRowIndexes<Entity.ItemTask>(itemTasksForDateEntity, newIndex, currentIndex);

            itemTaskEntity.RowIndex = newIndex;

            await _context.SaveChangesAsync();
        }

        public async Task<List<ItemTask>> GetActiveItemTasks(bool recurring)
        {
            Expression<Func<Entity.ItemTask, bool>> filter = i =>
                i.Item.Recurring.Equals(recurring) &&
                i.CompletionDate == null &&
                (i.CommittedDate == null || i.CommittedDate.Value.Date >= DateTime.Now.Date.AddDays(GlobalConstants.DaysRange));

            var itemTasksEntity = await _itemTaskRepository.GetAllAsync(
                filter: filter,
                orderBy: x => x.OrderBy(n => n.DueDate).ThenBy(n => n.Item.RowIndex),
                includeProperties: "Item"
                );

            return itemTasksEntity.Adapt<List<ItemTask>>();
        }

        public async Task<Dictionary<DateTime, List<Entity.ItemTask>>> GetCommitedItemsForNextWeek()
        {
            await UpdateExpiredItemTasks();

            var itemTasksEntity = await GetItemTasksGroupedByCommitDateForNextWeek();

            return itemTasksEntity;
        }

        private async Task UpdateExpiredItemTasks()
        {
            var today = DateTime.Now.Date;

            Expression<Func<Entity.ItemTask, bool>> filter = x =>
                x.CompletionDate == null &&
                x.CommittedDate.HasValue &&
                x.CommittedDate.Value.Date < today;

            var expiredItemTasksEntity = await _itemTaskRepository.GetAllAsync(filter);

            if (expiredItemTasksEntity.Any())
            {
                int newRowIndex = await GetNewItemTaskRowIndex(today);

                foreach (var task in expiredItemTasksEntity)
                {
                    task.CommittedDate = today;
                    task.RowIndex = newRowIndex++;
                }

                if (_context.ChangeTracker.HasChanges())
                {
                    await _context.SaveChangesAsync();
                }
            }
        }

        private async Task<Dictionary<DateTime, List<Entity.ItemTask>>> GetItemTasksGroupedByCommitDateForNextWeek()
        {
            var today = DateTime.Now.Date;
            var endOfDayRange = today.AddDays(GlobalConstants.DaysRange);

            Expression<Func<Entity.ItemTask, bool>> filter = x =>
                x.CompletionDate == null &&
                x.CommittedDate.HasValue &&
                x.CommittedDate.Value.Date >= today &&
                x.CommittedDate.Value.Date < endOfDayRange;

            var itemTasksForNextWeekEntity = await _itemTaskRepository.GetAllAsync(filter, includeProperties: "Item");

            var groupedItemTasksEntity = new Dictionary<DateTime, List<Entity.ItemTask>>();

            for (DateTime day = today; day < endOfDayRange; day = day.AddDays(1))
            {
                // commitani taskovi za specifičan dan
                var tasksForDay = itemTasksForNextWeekEntity
                    .Where(t =>
                        t.CommittedDate.HasValue &&
                        t.CommittedDate.Value.Date == day
                        )
                    .OrderBy(t => t.RowIndex)
                    .ToList();

                groupedItemTasksEntity.Add(day, tasksForDay);
            }

            return groupedItemTasksEntity;
        }

        private async Task<int> GetNewItemRowIndex(bool recurring)
        {
            var maxRowIndexItemEntity = await _itemRepository.GetFirstOrDefaultAsync(
                            x =>
                            !x.Completed &&
                            x.Recurring.Equals(recurring),
                            q => q.OrderByDescending(x => x.RowIndex)
                        );

            return maxRowIndexItemEntity?.RowIndex + 1 ?? 0;
        }

        private async Task<int> GetNewItemTaskRowIndex(DateTime? compareDate)
        {
            var maxRowIndexItemTaskEntity = await _itemTaskRepository.GetFirstOrDefaultAsync(
            x =>
                x.CompletionDate == null &&
                x.CommittedDate.HasValue &&
                compareDate.HasValue &&
                x.CommittedDate.Value.Date == compareDate.Value.Date,
            q => q.OrderByDescending(x => x.RowIndex)
            );

            return maxRowIndexItemTaskEntity?.RowIndex + 1 ?? 0;
        }

        private async Task UpdateRowIndexesForRemainingItems(Entity.Item itemEntity)
        {
            await _itemRepository.UpdateBatchAsync(
                x => !x.Completed &&
                      x.Recurring.Equals(itemEntity.Recurring) &&
                      x.RowIndex > itemEntity.RowIndex,
                x => new Entity.Item { RowIndex = x.RowIndex - 1 }
            );
        }

        private async Task UpdateItemTaskRowIndexesIfDateProvided(DateTime? oldCommittedDate, int? oldItemTaskRowIndex)
        {
            await _itemTaskRepository.UpdateBatchAsync(
                x => x.CompletionDate == null &&
                     x.CommittedDate.HasValue &&
                     oldCommittedDate.HasValue &&
                     x.CommittedDate.Value.Date == oldCommittedDate.Value.Date &&
                     x.RowIndex > oldItemTaskRowIndex,
                x => new Entity.ItemTask { RowIndex = x.RowIndex - 1 }
            );
        }
    }
}
