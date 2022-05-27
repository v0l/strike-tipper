import {v4 as uuidv4} from "uuid";

export class WebsocketEvents {
    constructor() {
        this.ids = new Set();
        this.handlers = {
            WidgetEvent: {}
        };
        this.Connect();
    }

    Connect() {
        let proto = window.location.protocol === "https:" ? "wss:" : "ws:";
        this.ws = new WebSocket(`${proto}//${window.location.host}/events`);

        this.ws.onopen = (ev) => {
            console.log("Events websocket open!");
            if (this.ids.size > 0) {
                this.SendCommand("listen", [...this.ids]);
            }
        };
        this.ws.onclose = (ev) => {
            console.log(ev);
            setTimeout(() => this.Connect(), 500);
        };
        this.ws.onerror = (ev) => {
            console.log(ev);
        };
        this.ws.onmessage = this.handleMessage.bind(this);
    }

    handleMessage(ev) {
        let msg = JSON.parse(ev.data);
        console.log(msg);

        for (let [type, hooks] of Object.entries(this.handlers)) {
            if (type !== msg.type) continue;

            for (let [_, fn] of Object.entries(hooks)) {
                fn(msg);
            }
        }
    }

    SendCommand(cmd, args) {
        if (this.ws.readyState === WebSocket.OPEN) {
            this.ws.send(JSON.stringify({
                cmd, args
            }));
        }
    }

    AddListenId(id) {
        if (!this.ids.has(id)) {
            this.ids.add(id);
            this.SendCommand("listen", [id]);
        }
    }

    HookEvent(type, handler) {
        let id = uuidv4();
        this.handlers[type][id] = handler;
        return id;
    }

    RemoveEvent(type, id) {
        delete this.handlers[type][id];
    }
}

export const Events = new WebsocketEvents();