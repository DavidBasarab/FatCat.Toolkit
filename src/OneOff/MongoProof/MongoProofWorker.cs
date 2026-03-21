using FatCat.Toolkit.Console;
using FatCat.Toolkit.Data.Mongo;

namespace OneOff.MongoProof;

public class MongoProofWorker(IMongoRepository<ProofItem> repository)
{
	public async Task DoWork()
	{
		ConsoleLog.WriteCyan("=== Mongo Proof: Testing constructor-injected repository (no Connect() call) ===");

		var item = new ProofItem
		{
			Name = $"proof-{Guid.NewGuid():N}",
			Value = 42,
		};

		ConsoleLog.WriteBlue($"Creating item | Name: {item.Name} | Value: {item.Value}");

		await repository.Create(item);

		ConsoleLog.WriteGreen($"Created | Id: {item.Id}");

		var retrieved = await repository.GetById(item.Id);

		if (retrieved == null)
		{
			ConsoleLog.WriteRed("FAIL: item not found after create");

			return;
		}

		ConsoleLog.WriteGreen($"Retrieved | Name: {retrieved.Name} | Value: {retrieved.Value}");

		await repository.Delete(retrieved);

		ConsoleLog.WriteGreen($"Deleted | Id: {retrieved.Id}");

		var afterDelete = await repository.GetById(item.Id);

		if (afterDelete != null)
		{
			ConsoleLog.WriteRed("FAIL: item still exists after delete");

			return;
		}

		ConsoleLog.WriteMagenta("=== PASS: create / retrieve / delete all succeeded ===");
	}
}
