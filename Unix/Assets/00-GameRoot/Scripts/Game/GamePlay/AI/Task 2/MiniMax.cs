using System;
using System.Collections.Generic;
using UnityEngine;


public class MiniMax : AI
{
    Dictionary<char, double> evaluationScoreLibrary = new Dictionary<char, double>()
    {
        {'M', 1 },  
        {'R', 2 },  
        {'W', 3 },  
        
    };
    public override void AssignUnits()
    {
        base.AssignUnits();         //the minimax algorthim gets to choose a team first and the genetic algorthim must take what's left over in an AI Battle
        
        GameData.STATIC_SetMinMaxColor(teamColor);
    }

    public override void Play()
    {

        GameManager.STATIC_SetAIEvaluationStatus(true);

        Algorithm(GameData.minMaxDepth,double.NegativeInfinity,double.PositiveInfinity, true, null);

        GameManager.STATIC_SetAIEvaluationStatus(false);

        bestMove.unit.Move(bestMove.target);

        GameManager.play -= Play;

    }

    double Evaluate(BaseUnit unit)
    {
        char characterCode;
        BaseUnit singletarget = null;
        List<BaseUnit> targets = new List<BaseUnit>();

        double evaluation = 0;
        characterCode = unit.characterID[1];

        bool simpleEval = characterCode == 'M' || characterCode == 'R' ? true : false;


        if (simpleEval)
        {
            singletarget = unit.CheckForEnemy();
            if (singletarget != null)
                evaluation += evaluationScoreLibrary[characterCode];
        }
        else
        {
            targets = unit.CheckForEnemies(true);

            foreach (BaseUnit target in targets)
            {
                characterCode = target.characterID[1];
                evaluation += evaluationScoreLibrary[characterCode];

            }

        }

        int perspective = unit.teamColor == teamColor ? 1 : -1; //the AI wants a high evaluation for itself and a low evaluation fot the other;
        evaluation *= perspective;

        return evaluation;

    }

    double Algorithm(int depth,double alpha, double beta, bool maximising, BaseUnit EvaluationUnit)
    {
        double evaluation;

        List<Tile> moves = new List<Tile>();

        if (depth == 0)
            return Evaluate(EvaluationUnit);

        if (maximising) // working with the AI units because they need the highest evaluation
        {
            double maxEval = double.NegativeInfinity; //this is the lowest possible evaluation 

            foreach (BaseUnit unit in aiUnits)
            {
                List<Tile> ValidMmoves = CheckValidMoves(unit); //every unit gets their moves checked to see if they have valid moves

                if (ValidMmoves != null) //if they have valid moves
                {
                    foreach (Tile tile in ValidMmoves)
                        moves.Add(tile);            // add them to an exterior list so that even though the unit will move and get a new set of avalable moves, the moves that this loop will use are not effected

                    if (moves.Count != 0) //just to check if the list isnt empty(MIGHT REMOVE)
                    {
                        Tile currentTile = unit.currentTile; //save the current tile that the unit is on at the moment

                        foreach (Tile targetTile in moves)
                        {

                            unit.Move(targetTile); //for every available move this unit has, move it.

                            evaluation = Algorithm(depth - 1,alpha,beta, false, unit); //loop through itself 

                            unit.Move(currentTile);

                            if (evaluation > maxEval)
                            {
                                maxEval = evaluation;

                                bestMove.unit = unit;
                                bestMove.target = targetTile;
                            }

                            alpha = Math.Max(alpha, evaluation);

                            if (beta <= alpha)
                                break;
                        }
                    }
                }
            }

            return maxEval;

        }else 
        {
            double minEval = double.PositiveInfinity; // the worst possible outcome for other units

            foreach (BaseUnit unit in otherUnits)
            {
                List<Tile> ValidMmoves = CheckValidMoves(unit); //working with the fact that the other has just gone; 

                if (ValidMmoves != null)
                {

                    foreach (Tile tile in ValidMmoves)
                        moves.Add(tile);

                    if (moves.Count != 0)
                    {
                        Tile currentTile = unit.currentTile;

                        foreach (Tile targetTile in moves)
                        {
                            unit.Move(targetTile);

                            evaluation = Algorithm(depth - 1, alpha, beta, true, unit); //loop through itself 

                            minEval = Math.Min(evaluation, minEval);

                            unit.Move(currentTile);

                            beta = Math.Min(beta, evaluation);

                            if (beta <= alpha)
                                break;

                        }
                    }
                }
            }

            return minEval;
        }

    }

    
}
