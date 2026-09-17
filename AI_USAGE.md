# AI Usage

## Tools used

- **Claude (Sonnet)** — used for architecture design, code generation, and review, via conversational back-and-forth rather than autocomplete.

## What I used it for

- Sketching the overall architecture (layering, EF Core config strategy, the deadlock-avoidance approach for locking two wallets)
- Designing the concurrency and idempotency test cases
- Drafting this README

## Example prompts and what came back

**Prompt:** "sketch the architecture"
**Result:** A full layered design including the deterministic-order row-locking strategy (`SELECT ... FOR UPDATE` on both wallets, always locked in ascending `Id` order regardless of transfer direction) to avoid deadlocks between concurrent opposite-direction transfers. This became the core of `WalletRepository.GetForUpdateAsync` and `TransferService`.

## A case where the AI's output was wrong/naive, and how I caught it

When I first asked for the models, Claude generated a DDD-style "rich domain model": a separate `Domain/` folder, kept deliberately EF-agnostic, with private setters and factory methods (`Wallet.Create(...)`, mutation only via `Credit()`/`Debit()` instance methods). This wasn't asked for, didn't match my project's actual folder structure (`Models/`, `Data/`, etc. — plain POCOs with EF fluent config in the DbContext), and added ceremony that wasn't necessary for a 48-hour take-home.

I pushed back ("why does the models look like this... i dont understand what Domain is for"), and the models were rebuilt as plain POCOs with public getters/setters in `Models/`, with the balance-safety invariant moved to where it actually needs to live for a concurrent system anyway, the service layer, enforced via row locking rather than inside the model itself. The lesson: default AI output tends toward textbook-correct patterns (DDD tactical patterns, in this case) that can be a mismatch for a specific project's conventions and time constraints. For a financial system specifically, this matters because over-engineering the wrong layer (the model) can create a false sense of safety the model's own invariant check only protects a single in-memory instance and does nothing against concurrent requests, which is worth stating explicitly rather than assuming a reviewer will infer it.

## Where I directed rather than accepted

- Rejected the `Domain/` EF-agnostic model layer in favor of plain `Models/` POCOs
- Chose pessimistic row locking over an AI-suggested-but-not-taken optimistic concurrency alternative, for easier live-demo verifiability
