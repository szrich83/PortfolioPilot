namespace PortfolioPilot

open WebSharper

[<JavaScript>]
module Simulation =

    // Convert annual percentage return into an approximate monthly growth rate
    let annualToMonthlyRate (annualReturnPercent: float) =
        annualReturnPercent / 100.0 / 12.0

    // Simulate long-term portfolio growth using monthly compounding
    let simulatePortfolioGrowth
        (initialCapital: float)
        (monthlyContribution: float)
        (years: int)
        (annualReturnPercent: float) =

        let months = max 0 (years * 12)
        let monthlyRate = annualToMonthlyRate annualReturnPercent

        // Recursive simulation loop storing portfolio value for each month
        let rec loop month currentValue acc =
            if month > months then
                List.rev acc
            else
                let nextValue =
                    if month = 0 then
                        currentValue
                    else
                        (currentValue + monthlyContribution) * (1.0 + monthlyRate)

                let point =
                    {
                        Month = month
                        Value = nextValue
                    }

                loop (month + 1) nextValue (point :: acc)

        loop 0 initialCapital []

    // Return the final portfolio value from the simulation timeline
    let getFinalValue (points: SimulationPoint list) =
        points
        |> List.tryLast
        |> Option.map (fun p -> p.Value)
        |> Option.defaultValue 0.0