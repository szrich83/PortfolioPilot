namespace PortfolioPilot

open WebSharper
open WebSharper.UI
open WebSharper.UI.Client
open WebSharper.UI.Html
open PortfolioPilot.Samples
open PortfolioPilot.PortfolioMetrics
open PortfolioPilot.Scoring

[<JavaScript>]
module Client =

    let private metricRow (title: string) (value: float) (suffix: string) =
        p [ attr.``class`` "metric-row" ] [
            text (sprintf "%s: %.2f%s" title value suffix)
        ]

    let private allocationRow (assets: Asset list) (allocation: PortfolioAllocation) =
        let assetName =
            assets
            |> List.tryFind (fun a -> a.Id = allocation.AssetId)
            |> Option.map (fun a -> a.Name)
            |> Option.defaultValue allocation.AssetId

        li [] [
            text (sprintf "%s - %.0f%%" assetName allocation.Percentage)
        ]

    let private portfolioCard (portfolio: Portfolio) =
        let metrics = calculatePortfolioMetrics sampleAssets portfolio
        let totalAllocation = allocationTotal portfolio
        let allocationOk = isAllocationValid portfolio

        div [ attr.``class`` "portfolio-card" ] [
            h3 [] [ text portfolio.Name ]

            p [] [
                text (sprintf "Total allocation: %.0f%%" totalAllocation)
            ]

            p [] [
                text (
                    if allocationOk then
                        "Allocation status: valid"
                    else
                        "Allocation status: invalid"
                )
            ]

            h4 [] [ text "Allocations" ]

            ul [] [
                for allocation in portfolio.Allocations do
                    allocationRow sampleAssets allocation
            ]

            h4 [] [ text "Calculated metrics" ]

            div [ attr.``class`` "metrics-box" ] [
                metricRow "Expected return" metrics.ExpectedReturn "%"
                metricRow "Risk score" metrics.Risk ""
                metricRow "Annual fee" metrics.Fee "%"
                metricRow "Liquidity" metrics.Liquidity ""
                metricRow "Diversification" metrics.Diversification ""
            ]
        ]

    let private rankingRow (position: int) (name: string) (score: float) =
        li [] [
            text (sprintf "%d. %s - Score: %.3f" position name score)
        ]

    [<SPAEntryPoint>]
    let Main () =
        let portfolioMetrics =
            samplePortfolios
            |> List.map (fun p -> (p, calculatePortfolioMetrics sampleAssets p))

        let ranking =
            scorePortfolios portfolioMetrics sampleCriteria

        let content =
            div [ attr.``class`` "page" ] [
                h1 [] [ text "PortfolioPilot" ]

                p [] [
                    text "A manual-data portfolio decision support and simulation tool."
                ]

                div [ attr.``class`` "summary-box" ] [
                    p [] [ text (sprintf "Sample assets: %d" (List.length sampleAssets)) ]
                    p [] [ text (sprintf "Sample portfolios: %d" (List.length samplePortfolios)) ]
                    p [] [ text (sprintf "Sample criteria: %d" (List.length sampleCriteria)) ]
                ]

                h2 [] [ text "Sample portfolios" ]

                div [ attr.``class`` "portfolio-grid" ] [
                    for portfolio in samplePortfolios do
                        portfolioCard portfolio
                ]

                h2 [] [ text "Ranking" ]

                div [ attr.``class`` "summary-box" ] [
                    ol [] [
                        for i, (name, score, _) in ranking |> List.indexed do
                            rankingRow (i + 1) name score
                    ]
                ]
            ]

        Doc.RunById "main" content