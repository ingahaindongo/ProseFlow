using ProseFlow.Core.Interfaces.Repositories;
using ProseFlow.Core.Models;

namespace ProseFlow.Infrastructure.Data.Repositories;

public class LocalModelRepository(AppDbContext context) : Repository<LocalModel>(context), ILocalModelRepository;