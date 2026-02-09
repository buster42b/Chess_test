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
        
        Container.BindInterfacesAndSelfTo<ChessBoard>()
            .FromComponentInNewPrefab(chessBoardPrefab)
            .AsSingle();

        Container.Bind<ChessMoveExecutor>().AsSingle();
        Container.Bind<PieceSetupController>().AsSingle();
        Container.Bind<ChessGameController>().AsSingle();
        Container.Bind<ChessClickHandler>().AsSingle();

        Container.Resolve<ChessBoard>().SetDependencies(
            Container.Resolve<ChessMoveExecutor>(),
            Container.Resolve<PieceSetupController>(),
            Container.Resolve<ChessGameController>());

        Container.Bind<ChessSaveLoadService>()
            .FromNewComponentOnNewGameObject()
            .AsSingle();
        Container.Bind<IInitializable>().To<ChessSaveLoadService>().FromResolve();

        if (uiPrefab)
        {
            var uiManager = Container.InstantiatePrefabForComponent<ChessUIManager>(uiPrefab);

            Container.Bind<ChessUIManager>()
                .FromInstance(uiManager)
                .AsSingle();

            Container.Bind<IInitializable>().To<ChessUIManager>().FromInstance(uiManager);
        }
        
        Container.Bind<ChessPieceFactory>()
            .AsSingle()
            .WithArguments(piecesConfig);
    } 
}
