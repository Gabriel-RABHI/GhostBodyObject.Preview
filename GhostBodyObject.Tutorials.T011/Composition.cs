using GhostBodyObject.Runtime.Logical.Model;
using Tutorials.T011.Billing;
using Tutorials.T011.Comments;
using Tutorials.T011.Comments.Mutations;
using Tutorials.T011.Billing.Mutations;

namespace Tutorials.T011;

// Only module selection; Catalog is a declared dependency of both modules, included exactly once.
[GhostRepository(110), IncludeModule(typeof(MCommentModule)), IncludeModule(typeof(MBillingModule))]
internal interface MCommerceRepository;

[GhostLog(111), IncludeModule(typeof(MBillingMutations)), IncludeModule(typeof(MCommentMutations))]
internal interface MCommerceLog;

[GhostRepository(112), IncludeModule(typeof(Tutorials.T011.Comments.Read.MCommentReadModule))]
internal interface MCommerceReadRepository;
