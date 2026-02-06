using UnityEngine;
using Zenject;

public class ChessInstaller : MonoInstaller
{
    [SerializeField] private GameObject chessBoardPrefab;
    [SerializeField] private ChessPiecesConfig piecesConfig;
    public override void InstallBindings()
    {
        var board = Container.InstantiatePrefabForComponent<ChessBoard>(chessBoardPrefab);
        
        Container.Bind<IBoard>().FromInstance(board).AsSingle();
        Container.Bind<ChessBoard>().FromInstance(board).AsSingle();
        Container.Bind<IInitializable>().To<ChessBoard>().FromInstance(board);

        Container.Bind<ChessPieceFactory>()
            .AsSingle()
            .WithArguments(piecesConfig);
    } 
}
