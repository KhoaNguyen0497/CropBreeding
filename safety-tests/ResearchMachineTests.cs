using CropBreeding.Core;
using StardewValley;
using StardewValley.GameData.Machines;
using SObject = StardewValley.Object;

namespace CropBreeding
{
    internal static class ResearchMachineTests
    {
        private static void Check(bool ok, string message) { if (!ok) throw new Exception(message); }
        internal static void Run()
        {
            ModEntry.Instance.Config = new ModConfig();
            var random = new CountedRandom();
            Random originalRandom = Game1.random;
            Game1.random = random;
            try
            {
                var data = ResearchMachine.CreateData();
                var rule = data.OutputRules.Single();
                Check(data.HasInput && data.HasOutput && !data.AllowFairyDust && rule.DaysUntilReady == -1
                    && rule.MinutesUntilReady == 240 && !rule.RecalculateOnCollect,
                    "native fixed four-hour machine, no fairy dust or collection rerolls");
                Check(data.WobbleWhileWorking && data.ShowNextIndexWhileWorking && !data.ShowNextIndexWhenReady,
                    "native working sprout and wobble; empty idle/ready frame");
                Check(rule.Triggers.Single().RequiredCount == 1 && rule.Triggers.Single().Condition == ResearchMachine.InputQuery,
                    "manual and Automate share one-seed validation and consumption");
                Check(rule.OutputItem.Single().OutputMethod == "CropBreeding.ResearchMachine, CropBreeding: CreateOutput", "native callback name matches public output method");
                var machine = new SObject { ItemId = ResearchMachine.MachineId };
                var seed = new SObject { ItemId = "473", Stack = 12, Quality = 2 };
                Traits.Write(seed.modData, ["researcher:1", "companion:3", "fast_growth:4"]);
                seed.modData[Companion.Key] = "24";
                var before = seed.modData.ToArray();
                Check(ResearchMachine.CanAccept(seed), "multi-trait Researcher seed accepted");
                for (int i = 0; i < 3; i++)
                    Check(ResearchMachine.CreateOutput(machine, seed, true, rule.OutputItem[0], out _) != null, "probe accepts without processing");
                Check(random.Calls == 0 && machine.heldObject.Value == null && seed.Stack == 12 && seed.modData.SequenceEqual(before),
                    "probes never roll, stage output or alter input");
                var output = ResearchMachine.CreateOutput(machine, seed, false, rule.OutputItem[0], out int? minutes);
                Check(output != null && output.ItemId == seed.ItemId && output.Stack == 1 && output.Quality == 2
                    && minutes == null && random.Calls == 2, "exactly two trait picks, same seed and native timing");
                Check(Traits.Level(output!.modData, "researcher") == 0 && output.modData[Companion.Key] == "24",
                    "Researcher removed and existing Companion assignment preserved");
                Check(seed.Stack == 12 && seed.modData.SequenceEqual(before), "output callback never consumes or edits the input itself");
                Check(!ResearchMachine.CanAccept(new SObject { ItemId = "473" }), "plain seed rejected");
                var crop = new SObject { ItemId = "24" }; Traits.Write(crop.modData, ["researcher:1"]);
                Check(!ResearchMachine.CanAccept(crop), "produce rejected even when it has Researcher");
                ModEntry.Instance.Config.MaximumTraits = 0;
                int calls = random.Calls;
                Check(ResearchMachine.CreateOutput(machine, seed, false, rule.OutputItem[0], out _) == null
                    && random.Calls == calls, "impossible input rejected without rolls or consumption");
                ModEntry.Instance.Config.MaximumTraits = 3;
                random.Throw = true;
                Check(ResearchMachine.CreateOutput(machine, seed, false, rule.OutputItem[0], out _) == null
                    && seed.Stack == 12 && seed.modData.SequenceEqual(before), "research error returns no output and preserves input");
            }
            finally { Game1.random = originalRandom; ModEntry.Instance.Config = new ModConfig(); }
            Console.WriteLine("Passed native research rule contract, probe safety, two-roll output, Companion preservation, invalid input and injected RNG errors. Uses test doubles; not live Automate.");
        }
        private sealed class CountedRandom : Random
        {
            internal int Calls;
            internal bool Throw;
            public override int Next(int maximum) { Calls++; if (Throw) throw new Exception("injected research RNG failure"); return 0; }
            public override double NextDouble() => throw new Exception("research must not roll a chance gate");
        }
    }
}

namespace StardewValley.GameData.Machines
{
    public enum MachineOutputTrigger { ItemPlacedInMachine }
    public sealed class MachineOutputTriggerRule
    {
        public MachineOutputTrigger Trigger;
        public int RequiredCount;
        public string Condition = "";
    }
    public sealed class MachineItemOutput
    {
        public string Id = "", OutputMethod = "";
        public int MinStack, MaxStack;
        public bool CopyQuality;
    }
    public sealed class MachineOutputRule
    {
        public string Id = "";
        public int DaysUntilReady;
        public int MinutesUntilReady;
        public bool RecalculateOnCollect;
        public List<MachineOutputTriggerRule> Triggers = [];
        public List<MachineItemOutput> OutputItem = [];
    }
    public sealed class MachineData
    {
        public bool HasInput, HasOutput, AllowFairyDust, WobbleWhileWorking, ShowNextIndexWhileWorking, ShowNextIndexWhenReady;
        public string InvalidItemMessage = "";
        public List<MachineOutputRule> OutputRules = [];
    }
}
