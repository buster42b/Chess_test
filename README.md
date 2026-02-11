# Chess_test

## Game Controls

### Mouse Controls
- **Left Mouse Button**: Click and drag to move chess pieces
- **Right Mouse Button**: Hold and drag to rotate the camera view
- **Mouse Wheel**: Scroll to zoom in/out

### Special Moves

#### Castling
- Castling is performed by moving the king **2 squares** left or right (towards the rook)
- The rook will automatically move to the appropriate position
- Standard castling rules apply (king and rook must not have moved, no pieces in between, king not in check)

#### En Passant
- En passant is performed by moving your pawn **behind** the opponent's pawn that just moved 2 squares forward
- This is the only way to perform en passant - collision-based captures are not supported
- Must be done immediately after the opponent's pawn moves 2 squares from its starting position

#### Pawn Promotion
- When a pawn reaches the opposite end of the board, it will automatically promote
- A promotion UI will appear allowing you to choose the piece (Queen, Rook, Bishop, or Knight)

### General Gameplay
- Pieces can be moved by clicking and dragging them to valid destination squares
- The game will only allow legal moves according to standard chess rules
- Turn-based gameplay with automatic player switching
- Visual feedback for valid moves and captures
