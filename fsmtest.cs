// I'm using C# records for this example.
public record Document(
    bool IsInDraft,
    bool HasBeenReviewedByLegal
    bool IsApproved,
    bool IsRejected,
    bool IsSentToCustomer,
    ...
    bool Is💩
);
if (document.IsInDraft)
{
    if (!document.HasBeenReviewedByLegal)
    {
        ...
    }
    else if (document.IsSentToCustomer && document.Approved || document.Rejected)
    {
        ...
    }
    else
    {
        ...
    }
}
...



using Stateless;

namespace DocumentWorkflow.FiniteStateMachine
{
    public class Document
    {
        private enum State
        {
            Draft,
            Review,
            ChangesRequested,
            SubmittedToClient,
            Approved,
            Declined
        }

        private enum Triggers
        {
            UpdateDocument,
            BeginReview,
            ChangedNeeded,
            Accept,
            Reject,
            Submit,
            Decline,
            RestartReview,
            Approve,
        }
    }
}



namespace DocumentWorkflow.FiniteStateMachine
{
    public class Document
    {
        private readonly StateMachine<State, Triggers> machine;

        private readonly StateMachine<State, Triggers>.TriggerWithParameters<string> changedNeededParameters;

        public Document()
        {
            // We can create the FSM with state stored in a file, DB, ORM wherever. In that case we'd need to create a factory
            // so the constructor isn't doing long/async work.
            //machine = new StateMachine<State, Triggers>(() => state, s => state = s);

            machine = new StateMachine<State, Triggers>(State.Draft);

            machine.Configure(State.Draft)
                .PermitReentry(Triggers.UpdateDocument)
                .Permit(Triggers.BeginReview, State.Review)
                .OnEntryAsync(OnDraftEntryAsync)
                .OnExitAsync(OnDraftExitAsync);

            changedNeededParameters = machine.SetTriggerParameters<string>(Triggers.ChangedNeeded);

            machine.Configure(State.Review)
                .Permit(Triggers.ChangedNeeded, State.ChangesRequested)
                .Permit(Triggers.Submit, State.SubmittedToClient)
                .OnEntryAsync(OnReviewEntryAsync)
                .OnExitAsync(OnReviewExitAsync);

            machine.Configure(State.ChangesRequested)
                .Permit(Triggers.Reject, State.Review)
                .Permit(Triggers.Accept, State.Draft)
                .OnEntryAsync(OnChangesRequestedEntryAsync)
                .OnExitAsync(OnChangesRequestedExitAsync);

            machine.Configure(State.SubmittedToClient)
                .Permit(Triggers.Approve, State.Approved)
                .Permit(Triggers.Decline, State.Declined)
                .OnEntryAsync(OnSubmittedToClientEnterAsync)
                .OnExitAsync(OnSubmittedToClientExitAsync);

            machine.Configure(State.Declined)
                .Permit(Triggers.RestartReview, State.Review)
                .OnEntryAsync(OnDeclinedEnterAsync)
                .OnExitAsync(OnDeclinedExitAsync);

            machine.Configure(State.Approved)
                .OnEntryAsync(OnApprovedEnter);
        }
    }
}



namespace DocumentWorkflow.FiniteStateMachine
{
    public class Document
    {
        private async Task OnDraftEntryAsync()
        {
            await notificationService.SendUpdateAsync(Priority.Verbose, "The proposal is now in the draft stage");
        }

        private async Task OnDraftExitAsync()
        {
            await notificationService.SendUpdateAsync(Priority.Verbose, "The proposal has now left the draft stage");
        }

        private async Task OnReviewEntryAsync()
        {
            await notificationService.SendUpdateAsync(Priority.Verbose, "The proposal is now in the review stage");
        }

        private async Task OnReviewExitAsync()
        {
            await notificationService.SendUpdateAsync(Priority.Verbose, "The proposal has now left the review stage");
        }

        // continued
    }
}


namespace DocumentWorkflow.FiniteStateMachine
{
    public class Document
    {
        public async Task UpdateDocumentAsync() => await machine.FireAsync(Triggers.UpdateDocument);

        public async Task BeginReviewAsync() => await machine.FireAsync(Triggers.BeginReview);

        public async Task MakeChangeAsync(string change) => await machine.FireAsync(changedNeededParameters, change);

        public async Task AcceptAsync() => await machine.FireAsync(Triggers.Accept);

        public async Task RejectAsync() => await machine.FireAsync(Triggers.Reject);

        public async Task SubmitAsync() => await machine.FireAsync(Triggers.Submit);

        public async Task RestartReviewAsync() => await machine.FireAsync(Triggers.RestartReview);

        public async Task ApproveAsync() => await machine.FireAsync(Triggers.Approve);

        public async Task DeclineAsync() => await machine.FireAsync(Triggers.Decline);
    }
}


machine = new StateMachine<State, Triggers>(State.Draft);

machine.Configure(State.Draft)
    .PermitReentry(Triggers.UpdateDocument)
    .Permit(Triggers.BeginReview, State.Review)
    .OnEntryAsync(OnDraftEntryAsync)
    .OnExitAsync(OnDraftExitAsync);

machine.Configure(State.Review)
    .Permit(Triggers.ChangedNeeded, State.ChangesRequested)
    .Permit(Triggers.Submit, State.SubmittedToClient)
    .OnEntryFromAsync(Triggers.Decline, OnEntryFromDeclinedAsync)
    .OnEntryAsync(OnReviewEntryAsync)
    .OnExitAsync(OnReviewExitAsync);

var document = new Document();

// Let's try be sneaky and submit the document to the client before it's even been reviewed!
await document.SubmitAsync();
System.InvalidOperationException
  HResult=0x80131509
  Message=No valid leaving transitions are permitted from state 'Draft' for trigger 'Submit'. Consider ignoring the trigger.
  Source=Stateless
  StackTrace:
   at Stateless.StateMachine`2.DefaultUnhandledTriggerAction(TState state, TTrigger trigger, ICollection`1 unmetGuardConditions)
   at Stateless.StateMachine`2.UnhandledTriggerAction.Sync.Execute(TState state, TTrigger trigger, ICollection`1 unmetGuards)
   at Stateless.StateMachine`2.UnhandledTriggerAction.Sync.ExecuteAsync(TState state, TTrigger trigger, ICollection`1 unmetGuards)
  ...


  machine.OnUnhandledTrigger((state, trigger) => notificationService.SendUpdateAsync(
    Priority.Blocking,
    $"Document is currently in \"{state}\". There are no valid exit transitions from this stage for trigger \"{trigger}\"."));

// Proposal is currently in "Draft". There are no valid exit transitions from this stage for trigger "Submit".



string graph = UmlDotGraph.Format(document.GetInfo());

// Our document workflow becomes:
digraph {
    compound=true;
    node [shape=Mrecord]
    rankdir="LR"
    "Draft" [label="Draft|entry / OnDraftEntryAsync\nexit / OnDraftExitAsync"];
    "Review" [label="Review"];
...



