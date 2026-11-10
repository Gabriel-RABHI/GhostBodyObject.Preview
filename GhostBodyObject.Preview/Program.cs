using System.Security.Cryptography.X509Certificates;

Console.WriteLine("Hello, World! GBO is comming. Keep connected!");

// ------------------------------------------------------------------------------------------------ //
// -------- Type System
public enum StoreSystemRole
{
    State,
    Projection,
    InBox,
    OutBox,
    ReplayableLog,
    Log
}

// ******** //
public class GhostRepositoryAttribute(ushort TypeId) : Attribute;
public class GhostLogAttribute(ushort TypeId) : Attribute;
public class GhostInBoxAttribute(ushort TypeId) : Attribute;
public class GhostOutBoxAttribute(ushort TypeId) : Attribute;
public class GhostBoxAttribute(string inBox, string outBox) : Attribute;

// ******** //
public class SystemModelAttribute(ushort TypeId) : Attribute;

[AttributeUsage(AttributeTargets.Interface, AllowMultiple = true)]
public class SystemModelIncludeAttribute(string model, StoreSystemRole role, string storeName) : Attribute;
public class SystemModelAggregateAttribute(string model) : Attribute;

// ******** //
public class StoreSystemAttribute(ushort TypeId) : Attribute;
public class StoreSystemIncludeAttribute(string storeSystemName) : Attribute;
public class StoreSystemAggregateAttribute(string storeSystemName) : Attribute;


// *****************************************************************************************
// ********************************* Application.Common  **********************************
// ***************************************************************************************** 
public static class ApplicationSystemModelNames
{
    // ---------- FRONT
    public const string FrontStateName = "FrontState";

    // -------- DOMAIN / FRONT
    public const string MutationInBoxName = "MutationIn";

    public const string MutationOutBoxName = "MutationOut";

    // -------- DOMAIN
    public const string MutationLogName = "Mutation";

    public const string WriteModelName = "Write";

    public const string ReadModelName = "Read";

    // ---------- SERVICES / DOMAIN
    public const string ServiceInBoxName = "ServiceIn";

    public const string ServiceOutBoxName = "ServiceOut";

    // ---------- SERVICES
    public const string ServiceStateName = "Service";
}

// *****************************************************************************************
// ********************************* Application.Front  **********************************
// *****************************************************************************************
[SystemModel(1)]
[SystemModelAggregate(nameof(MTodoListDomainSystemModel))]
public interface MFrontSystemModel
{

}

[StoreSystem(2)]
[StoreSystemAggregate(nameof(MTodoListDomainSystemModel))]
public interface MFrontStoreSystem
{

}

// *****************************************************************************************
// ********************************* Application.Domain  **********************************
// *****************************************************************************************
[SystemModel(1)]
[SystemModelAggregate(nameof(MTodoListDomainSystemModel))]
public interface MDomainSystemModel
{

}

[StoreSystem(2)]
[StoreSystemAggregate(nameof(MTodoListDomainSystemModel))]
public interface MDomainStoreSystem
{

}

// *****************************************************************************************
// ********************************* Application.TodoList.Domain  **********************************
// *****************************************************************************************
[SystemModel(1)]
[SystemModelInclude(nameof(MTodoListMutationInBox), StoreSystemRole.InBox, ApplicationSystemModelNames.MutationInBoxName)]
[SystemModelInclude(nameof(MTodoListMutationInBox), StoreSystemRole.OutBox, ApplicationSystemModelNames.MutationOutBoxName)]
[SystemModelInclude(nameof(MTodoListWriteModelRepository), StoreSystemRole.Projection, ApplicationSystemModelNames.WriteModelName)]
[SystemModelInclude(nameof(MTodoListReadModelRepository), StoreSystemRole.Projection, ApplicationSystemModelNames.ReadModelName)]
[SystemModelInclude(nameof(MTodoListMutationLog), StoreSystemRole.ReplayableLog, ApplicationSystemModelNames.MutationLogName)]
public interface MTodoListDomainSystemModel
{

}

[StoreSystem(2)]
[StoreSystemInclude(nameof(MTodoListDomainSystemModel))]
public interface MTodoListDomainStoreSystem
{

}

// ------------------------------------------------------------------------------------- //
[GhostInBox(3)]
public interface MTodoListMutationInBox
{

}

[GhostOutBox(4)]
public interface MTodoListMutationOutBox
{

}

[GhostBox(nameof(MTodoListMutationInBox), nameof(MTodoListMutationInBox))]
public interface MTodoListMutationBox
{

}

[GhostRepository(5)]
public interface MTodoListWriteModelRepository
{

}

[GhostRepository(6)]
public interface MTodoListReadModelRepository
{

}

[GhostLog(7)]
public interface MTodoListMutationLog
{

}


// *****************************************************************************************
// ********************************* Application.TodoList.Front  **********************************
// *****************************************************************************************
[SystemModel(1)]
[SystemModelInclude(nameof(MTodoListFrontMutationInBox), StoreSystemRole.InBox, ApplicationSystemModelNames.MutationInBoxName)]
[SystemModelInclude(nameof(MTodoListFrontMutationOutBox), StoreSystemRole.OutBox, ApplicationSystemModelNames.MutationOutBoxName)]
[SystemModelInclude(nameof(MTodoListFrontStateRepository), StoreSystemRole.State, ApplicationSystemModelNames.WriteModelName)]
public interface MTodoListFrontSystemModel
{

}

[StoreSystem(2)]
[StoreSystemInclude(nameof(MTodoListDomainSystemModel))]
public interface MTodoListFrontStoreSystem
{

}

// ------------------------------------------------------------------------------------- //
[GhostInBox(3)]
public interface MTodoListFrontMutationInBox
{

}

[GhostOutBox(4)]
public interface MTodoListFrontMutationOutBox
{

}

[GhostBox(nameof(MTodoListFrontMutationOutBox), nameof(MTodoListFrontMutationInBox))]
public interface MTodoListFrontMutationBox
{

}

[GhostRepository(6)]
public interface MTodoListFrontStateRepository
{

}

// *****************************************************************************************
// ********************************* Application.User.Domain  **********************************
// ***************************************************************************************** 



// *****************************************************************************************
// ********************************* Application.Service.Mailling  **********************************
// *****************************************************************************************
[SystemModel(1)]
[SystemModelInclude(nameof(MMaillingJobInBox), StoreSystemRole.InBox, ApplicationSystemModelNames.ServiceInBoxName)]
[SystemModelInclude(nameof(MMaillingJobOutBox), StoreSystemRole.OutBox, ApplicationSystemModelNames.ServiceOutBoxName)]
[SystemModelInclude(nameof(MMaillingRepository), StoreSystemRole.State, ApplicationSystemModelNames.ServiceStateName)]
public interface MMaillingSystemModel
{

}

[StoreSystem(2)]
[StoreSystemInclude(nameof(MTodoListDomainSystemModel))]
public interface MMaillingStoreSystem
{

}

// ------------------------------------------------------------------------------------- //
[GhostInBox(3)]
public interface MMaillingJobInBox
{

}

[GhostOutBox(4)]
public interface MMaillingJobOutBox
{

}

[GhostBox(nameof(MMaillingJobOutBox), nameof(MMaillingJobInBox))]
public interface MMaillingJobBox
{

}

[GhostRepository(6)]
public interface MMaillingRepository
{

}
