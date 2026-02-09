using UnityEngine;
using Zenject;

public class ChessInstaller : MonoInstaller
{
    [SerializeField] private GameObject chessBoardPrefab;
    [SerializeField] private ChessPiecesConfig piecesConfig;
    [SerializeField] private ChessUIManager uiPrefab;
    [SerializeField] private ChessClickHandler sceneInteractionHandler;

    public override void InstallBindings()
    {
        

        if (sceneInteractionHandler != null)
            Container.Bind<IInteractionHandler>().FromInstance(sceneInteractionHandler).AsSingle();
        
        var board = Container.InstantiatePrefabForComponent<ChessBoard>(chessBoardPrefab);
        Container.Bind<IBoard>().FromInstance(board).AsSingle();
        Container.Bind<ChessBoard>().FromInstance(board).AsSingle();
        Container.Bind<IInitializable>().To<ChessBoard>().FromInstance(board);

        if (uiPrefab)
        {
            ChessUIManager uiManager;

            uiManager = Container.InstantiatePrefabForComponent<ChessUIManager>(uiPrefab);

            Container.Bind<ChessUIManager>()
            .FromInstance(uiManager)
            .AsSingle();

            Container.Bind<IInitializable>().To<ChessUIManager>().FromInstance(uiManager);
        }

        Container.Bind<ChessMoveExecutor>().AsSingle();
        Container.Bind<PieceSetupController>().AsSingle();
        Container.Bind<ChessGameController>().AsSingle();

        board.SetDependencies(
            Container.Resolve<ChessMoveExecutor>(),
            Container.Resolve<PieceSetupController>(),
            Container.Resolve<ChessGameController>());

        Container.Bind<ChessSaveLoadService>()
            .FromNewComponentOnNewGameObject()
            .AsSingle();
        Container.Bind<IInitializable>().To<ChessSaveLoadService>().FromResolve();

        Container.Bind<ChessPieceFactory>()
            .AsSingle()
            .WithArguments(piecesConfig);
    } 
}
