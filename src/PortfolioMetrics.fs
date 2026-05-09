namespace PortfolioPilot

open WebSharper

[<JavaScript>]
module PortfolioMetrics =

    // Find the asset referenced by a portfolio allocation
    let private tryFindAsset (assets: Asset list) (assetId: string) =
        assets |> List.tryFind (fun a -> a.Id = assetId)

    // Calculate a weighted average value for a selected asset metric
    let private weightedValue
        (assets: Asset list)
        (allocations: PortfolioAllocation list)
        (selector: Asset -> float) =

        allocations
        |> List.sumBy (fun allocation ->
            match tryFindAsset assets allocation.AssetId with
            | Some asset ->
                let weight = allocation.Percentage / 100.0
                weight * selector asset

            | None ->
                0.0
        )

    // Aggregate asset-level values into portfolio-level metrics
    let calculatePortfolioMetrics
        (assets: Asset list)
        (portfolio: Portfolio) : PortfolioMetric =

        {
            ExpectedReturn =
                weightedValue assets portfolio.Allocations (fun a -> a.ExpectedAnnualReturn)

            Risk =
                weightedValue assets portfolio.Allocations (fun a -> a.RiskScore)

            Fee =
                weightedValue assets portfolio.Allocations (fun a -> a.AnnualFee)

            Liquidity =
                weightedValue assets portfolio.Allocations (fun a -> a.LiquidityScore)

            Diversification =
                weightedValue assets portfolio.Allocations (fun a -> a.DiversificationScore)
        }

    // Total allocation percentage of a portfolio
    let allocationTotal (portfolio: Portfolio) =
        portfolio.Allocations
        |> List.sumBy (fun a -> a.Percentage)

    // A valid portfolio must allocate exactly 100% of capital
    let isAllocationValid (portfolio: Portfolio) =
        abs (allocationTotal portfolio - 100.0) < 0.0001