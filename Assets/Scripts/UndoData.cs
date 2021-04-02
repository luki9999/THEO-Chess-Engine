using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static ChessBoard;

public struct UndoMoveData{
    public int start;
    public int end;
    public int movedPiece;
    public int takenPiece;
    public int castlingIndex;
    public bool[] castlingBefore;
    public int epSpaceBefore;
    public bool wasPromotion;



    public UndoMoveData(int start, int end, int movedPiece, int takenPiece, int castlingIndex, bool[] castlingBefore, int epSpaceBefore, bool wasPromotion){
        this.start = start;
        this.end = end;
        this.movedPiece = movedPiece;
        this.takenPiece = takenPiece;
        this.castlingIndex = castlingIndex;
        this.castlingBefore = castlingBefore;
        this.epSpaceBefore = epSpaceBefore;
        this.wasPromotion = wasPromotion;
    }

    public UndoMoveData(int start, int end, MoveGenerator moveGenerator){ //do this before actually making the move
        this.start = start;
        this.movedPiece = moveGenerator.board[start];
        this.end = end;
        this.takenPiece = moveGenerator.board[end];
        this.castlingBefore = (bool[]) moveGenerator.gameData.castling.Clone();
        this.epSpaceBefore = moveGenerator.gameData.epSpace;
        castlingIndex = -1;
        if((SpaceY(end) == 0 || SpaceY(end) == 7) && PieceType(movedPiece) == pawn){ // kinda slow but gets mostly skipped
            wasPromotion = true;
            movedPiece = PieceInt(queen, PieceColor(movedPiece));
        } else {
            wasPromotion = false;
        }
        if(PieceType(movedPiece) == king && Mathf.Abs(start-end) == 2){ // castling
            for(int i = 0; i < 4; i++){
                if(kingsAfter[i] == end){
                    castlingIndex = i;
                }
            }
        }
    }
}

//oh god what a waste of time
//i dont know why this doesnt work, but its stupid anyway
/*
public struct MoveData
{
    byte dataByte;
    // bit 0, 1, 2, 3: castling
    // bit 4, 5, 6, 7: e. p. Space
    public uint DataInt { get => dataByte; }
    public bool[] Castling { get => GetCastlingArray(); set => SetCastlingBits(value); }
    public uint EPSpace { get => GetEPSpace(); set => SetEPSpace(value); }

    public MoveData(uint dataInt)
    {
        dataByte = (byte)dataInt;
    }

    bool[] GetCastlingArray()
    {
        var output = new bool[4];
        for (int i = 0; i < 4; i++)
        {
             output[i] = (dataByte & (1 << i)) != 0;
        }
        return output;
    }

    void SetCastlingBits(bool[] input)
    {
        for (int i = 0; i < 4; i++)
        {
            if (input[i]) dataByte |= (byte)(1 << i);
        }
    }

    uint GetEPSpace()
    {
        uint epIndex = (uint) (dataByte >> 4);
        uint row = (epIndex >> 3 == 0) ? 2u : 5u;
        return row << 3 | epIndex & 0b111; 
    }

    void SetEPSpace(uint input)
    {
        uint row = ((input >> 3) == 5) ? 8u : 0u; //maybe optimize with bitshifts, easy readable for now
        uint epIndex = (input & 0b111) | row;
        dataByte |= (byte)(epIndex << 4);
    }
}

public struct MoveNew
{
    ulong moveInt; // 1 byte start, 1 byte end, 1 byte movedPiece, 1 byte capturedPiece, 1 byte data, 1 bit player, 1 bit isCapture, 1 bit isCastling, 1 bit isPromotion, 4 bits castlingIndex, 1 bit alreadyMade

    //dont set anything twice, only works if moveInt = 0
    public ulong Start { 
        get => (moveInt & 0b1111_1111);
        set => moveInt |= value; 
    }
    public ulong End { 
        get => ((moveInt >> 8) & 0b1111_1111);
        set => moveInt |= (value << 8);
    }
    public ulong MovedPiece { 
        get => ((moveInt >> 16) & 0b1111_1111);
        set => moveInt |= (value << 16);
    }
    public ulong TakenPiece { 
        get => ((moveInt >> 24) & 0b1111_1111);
        set => moveInt |= (value << 24);
    }
    public MoveData Data { 
        get => new MoveData((uint)((moveInt >> 32) & 0b1111_1111));
        set => moveInt |= (value.DataInt << 32);
    }
    public int Player { 
        get => (int) (moveInt >> 33) & 0b1;
        set => moveInt |= (((ulong)value) << 33);
    }
    public bool IsCapture {
        get => ((moveInt >> 34) & 0b1) == 1;
        set => moveInt |= (((value) ? 1ul : 0ul) << 34);
    }
    public bool IsCastling { 
        get => ((moveInt >> 35) & 0b1) == 1;
        set => moveInt |= (((value) ? 1ul : 0ul) << 35);
    }
    public int CastlingIndex { 
        get => (int)((moveInt >> 36) & 0b1111);
        set => moveInt |= (((ulong)value) << 36);
    }
    public bool AlreadyMade { 
        get => ((moveInt >> 40) & 0b1) == 1;
        set => moveInt |= (((value) ? 1ul : 0ul) << 40);
    }
    public bool IsPromotion {
        get => ((moveInt >> 41) & 0b1) == 1;
        set => moveInt |= (((value) ? 1ul : 0ul) << 41);
    }


    public MoveNew (uint start, uint end, uint movedPiece, uint takenPiece, MoveData data, uint player, bool isCapture, bool isCastling, bool isPromotion, uint castlingIndex,  bool alreadyMade)
    {
        uint captureBit = (isCapture) ? 1u : 0u;
        uint castlingBit = (isCastling) ? 1u : 0u;
        uint alredyMadeBit = (alreadyMade) ? 1u : 0u;
        uint promotionBit = (isPromotion) ? 1u : 0u;
        moveInt = (ulong)start | end << 8 | movedPiece << 16 | takenPiece << 24 | data.DataInt << 32 | player << 33 | captureBit << 34| castlingBit << 35 | castlingIndex << 36 | alredyMadeBit << 40 | promotionBit << 41;
    }

    public MoveNew(ulong newMoveInt = 0)
    {
        moveInt = newMoveInt;
    }

}
*/