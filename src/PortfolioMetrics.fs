namespace PortfolioPilot

open WebSharper

[<JavaScript>]
module PortfolioMetrics =

    let private tryFindAsset (assets: Asset list) (assetId: string) =
        assets |> List.tryFind (fun a -> a.Id = assetId)

    let private weightedValue (assets: Asset list) (allocations: PortfolioAllocation list) (selector: Asset -> float) =
        allocations
        |> List.sumBy (fun allocation ->
            match tryFindAsset assets allocation.AssetId with
            | Some asset ->
                let weight = allocation.Percentage / 100.0
                weight * selector asset
            | None ->
                0.0
        )

    let calculatePortfolioMetrics (assets: Asset list) (portfolio: Portfolio) : PortfolioMetric =
        {
            ExpectedReturn = weightedValue assets portfolio.Allocations (fun a -> a.ExpectedAnnualReturn)
            Risk = weightedValue assets portfolio.Allocations (fun a -> a.RiskScore)
            Fee = weightedValue assets portfolio.Allocations (fun a -> a.AnnualFee)
            Liquidity = weightedValue assets portfolio.Allocations (fun a -> a.LiquidityScore)
            Diversification = weightedValue assets portfolio.Allocations (fun a -> a.DiversificationScore)
        }

    let allocationTotal (portfolio: Portfolio) =
        portfolio.Allocations |> List.sumBy (fun a -> a.Percentage)

    let isAllocationValid (portfolio: Portfolio) =
        abs (allocationTotal portfolio - 100.0) < 0.0001