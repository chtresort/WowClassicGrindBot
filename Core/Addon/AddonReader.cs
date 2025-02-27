using Core.Database;

using Microsoft.Extensions.Logging;

using SharedLib;

using System;
using System.Threading;

namespace Core;

public sealed class AddonReader : IAddonReader, IDisposable
{
    private readonly ILogger logger;
    private readonly IAddonDataProvider reader;
    private readonly AutoResetEvent resetEvent;

    public PlayerReader PlayerReader { get; }

    public CombatLog CombatLog { get; }

    public BagReader BagReader { get; }
    public EquipmentReader EquipmentReader { get; }

    public ActionBarCostReader ActionBarCostReader { get; }

    public ActionBarCooldownReader ActionBarCooldownReader { get; }

    public AuraTimeReader PlayerBuffTimeReader { get; }

    public AuraTimeReader TargetDebuffTimeReader { get; }

    public AuraTimeReader TargetBuffTimeReader { get; }

    public ActionBarBits CurrentAction { get; }
    public ActionBarBits UsableAction { get; }

    public GossipReader GossipReader { get; }

    public SpellBookReader SpellBookReader { get; }
    public TalentReader TalentReader { get; }

    public event Action? AddonDataChanged;

    public WorldMapAreaDB WorldMapAreaDb { get; }

    public ItemDB ItemDb { get; }
    public CreatureDB CreatureDb { get; }
    public AreaDB AreaDb { get; }

    public RecordInt GlobalTime { get; } = new(98);

    public int DamageTakenCount() => CombatLog.DamageTaken.Count;
    public int DamageDoneCount() => CombatLog.DamageDone.Count;

    private int lastTargetGuid = -1;
    public string TargetName { get; private set; } = string.Empty;

    private int lastMouseOverId = -1;
    public string MouseOverName { get; private set; } = string.Empty;

    public double AvgUpdateLatency { private set; get; }
    private double updateSum;
    private int updateIndex;
    private DateTime lastUpdate;

    public AddonReader(ILogger logger, IAddonDataProvider reader, PlayerReader playerReader,
        AutoResetEvent resetEvent, AreaDB areaDB, WorldMapAreaDB worldMapAreaDB,
        ItemDB itemDB, CreatureDB creatureDB, SpellDB spellDB, TalentDB talentDB)
    {
        this.logger = logger;
        this.reader = reader;
        this.resetEvent = resetEvent;

        this.AreaDb = areaDB;
        this.WorldMapAreaDb = worldMapAreaDB;
        this.ItemDb = itemDB;
        this.CreatureDb = creatureDB;

        this.CombatLog = new(64, 65, 66, 67);

        this.EquipmentReader = new(ItemDb, 23, 24);
        this.BagReader = new(ItemDb, EquipmentReader, 20, 21, 22);

        this.ActionBarCostReader = new(35, 36);
        this.ActionBarCooldownReader = new(37);

        this.GossipReader = new(73);

        this.SpellBookReader = new(71, spellDB);

        this.PlayerReader = playerReader;
        this.TalentReader = new(72, PlayerReader, talentDB);

        this.CurrentAction = new(25, 26, 27, 28, 29);
        this.UsableAction = new(30, 31, 32, 33, 34);

        this.PlayerBuffTimeReader = new(79, 80);
        this.TargetDebuffTimeReader = new(81, 82);
        this.TargetBuffTimeReader = new(83, 84);

        lastUpdate = DateTime.UtcNow;
    }

    public void Dispose()
    {
        BagReader.Dispose();
    }

    public void Update()
    {
        FetchData();

        if (!GlobalTime.UpdatedNoEvent(this.reader))
            return;

        if (GlobalTime.Value <= 3)
        {
            updateSum = 0;
            updateIndex = 0;

            FullReset();
            return;
        }
        else if (updateIndex >= 512)
        {
            updateSum = 0;
            updateIndex = 0;
        }

        resetEvent.Reset();

        updateSum += (DateTime.UtcNow - lastUpdate).TotalMilliseconds;
        updateIndex++;
        AvgUpdateLatency = updateSum / updateIndex;
        lastUpdate = DateTime.UtcNow;

        IAddonDataProvider reader = this.reader;

        CurrentAction.Update(reader);
        UsableAction.Update(reader);

        PlayerReader.Update(reader);

        if (lastTargetGuid != PlayerReader.TargetGuid)
        {
            lastTargetGuid = PlayerReader.TargetGuid;

            TargetName =
                CreatureDb.Entries.TryGetValue(PlayerReader.TargetId, out string? name)
                ? name
                : reader.GetString(16) + reader.GetString(17);
        }

        if (lastMouseOverId != PlayerReader.MouseOverId)
        {
            lastMouseOverId = PlayerReader.MouseOverId;
            MouseOverName =
                CreatureDb.Entries.TryGetValue(PlayerReader.MouseOverId, out string? name)
                ? name
                : string.Empty;
        }

        CombatLog.Update(reader, PlayerReader.Bits.PlayerInCombat());

        BagReader.Read(reader);
        EquipmentReader.Read(reader);

        ActionBarCostReader.Read(reader);
        ActionBarCooldownReader.Read(reader);

        GossipReader.Read(reader);

        SpellBookReader.Read(reader);
        TalentReader.Read(reader);

        PlayerBuffTimeReader.Read(reader);
        TargetDebuffTimeReader.Read(reader);
        TargetBuffTimeReader.Read(reader);

        AreaDb.Update(WorldMapAreaDb.GetAreaId(PlayerReader.UIMapId.Value));

        resetEvent.Set();
    }

    public void FetchData()
    {
        reader.Update();
    }

    public void SessionReset()
    {
        CombatLog.Reset();
    }

    public void FullReset()
    {
        PlayerReader.Reset();

        ActionBarCostReader.Reset();
        ActionBarCooldownReader.Reset();
        SpellBookReader.Reset();
        TalentReader.Reset();

        PlayerBuffTimeReader.Reset();
        TargetDebuffTimeReader.Reset();
        TargetBuffTimeReader.Reset();

        SessionReset();
    }

    public int GetInt(int index)
    {
        return reader.GetInt(index);
    }

    public void UpdateUI()
    {
        AddonDataChanged?.Invoke();
    }
}