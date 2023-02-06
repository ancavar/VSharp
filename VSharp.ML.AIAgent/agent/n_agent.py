from common.game import GameMap
from common.game import GameState
from common.messages import Reward
from common.messages import ClientMessage
from common.messages import StartMessageBody
from common.messages import StepMessageBody
from common.messages import GetAllMapsMessageBody
from common.messages import ServerMessage
from common.messages import ServerMessageType
from common.messages import MapsServerMessage
from common.messages import GameOverServerMessage
from common.messages import GameStateServerMessage
from common.messages import RewardServerMessage
from .connection_manager import ConnectionManager


def get_server_maps(cm: ConnectionManager) -> list[GameMap]:
    ws = cm.issue()
    request_all_maps_message = ClientMessage(GetAllMapsMessageBody())
    ws.send(request_all_maps_message.to_json())
    maps_message = ws.recv()

    cm.release(ws)
    return MapsServerMessage.from_json(maps_message).MessageBody.Maps


class NAgent:
    """
    агент для взаимодействия с сервером
    - отслеживает состояние общения
    - ловит и кидает ошибки
    - делает шаги

    исползует потокобезопасную очередь
    """

    class WrongAgentStateError(Exception):
        def __init__(
            self, source: str, received: str, expected: str, at_step: int
        ) -> None:
            super().__init__(
                f"Wrong operations order at step #{at_step}: at function <{source}> received {received}, expected {expected}"
            )

    class IncorrectSentStateError(Exception):
        pass

    class GameOver(Exception):
        pass

    def __init__(
        self,
        cm: ConnectionManager,
        map_id_to_play: int,
        steps: int,
        log: bool = False,
    ) -> None:
        self.cm = cm
        self._ws = cm.issue()
        self.log = log

        start_message = ClientMessage(
            StartMessageBody(MapId=map_id_to_play, StepsToPlay=steps)
        )
        if log:
            print("-->", start_message, "\n")
        self._ws.send(start_message.to_json())
        self._current_step = 0
        self.game_is_over = False

    def _raise_if_gameover(self, msg) -> GameOverServerMessage | str:
        if self.game_is_over:
            raise NAgent.GameOver
        match ServerMessage.from_json(msg).MessageType:
            case ServerMessageType.GAMEOVER:
                self.game_is_over = True
                raise NAgent.GameOver
            case _:
                return msg

    def recv_state_or_throw_gameover(self) -> GameState:
        received = self._ws.recv()
        data = GameStateServerMessage.from_json(self._raise_if_gameover(received))
        return data

    def send_step(self, next_state_id: int, predicted_usefullness: int):
        if self.log:
            print(f"{next_state_id=}")
        do_step_message = ClientMessage(
            StepMessageBody(
                StateId=next_state_id, PredictedStateUsefulness=predicted_usefullness
            )
        )
        if self.log:
            print("-->", do_step_message)
        self._ws.send(do_step_message.to_json())
        self._sent_state_id = next_state_id

    def recv_reward_or_throw_gameover(self) -> Reward:
        data = RewardServerMessage.from_json(self._raise_if_gameover(self._ws.recv()))
        if self.log:
            print("<--", data.MessageType, end="\n\n")

        return self._process_reward_server_message(data)

    def _process_reward_server_message(self, msg):
        match msg.MessageType:
            case ServerMessageType.INCORRECT_PREDICTED_STATEID:
                raise NAgent.IncorrectSentStateError(
                    f"Sending state_id={self._sent_state_id} at step #{self._current_step} resulted in {msg.MessageType}"
                )

            case ServerMessageType.MOVE_REVARD:
                self._current_step += 1
                return msg.MessageBody

            case _:
                raise RuntimeError(
                    f"Unexpected message type received: {msg.MessageType}"
                )

    def close(self):
        self.cm.release(self._ws)