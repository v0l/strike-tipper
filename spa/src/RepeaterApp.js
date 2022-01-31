import './App.css';
import {useState} from "react";
import {TipperWidget} from "./TipperWidget";

export function RepeaterApp() {
    let [username, setUsername] = useState();
    let [config, setConfig] = useState();

    function handleInput(e) {
        setUsername(e.target.value);
    }

    async function handleSubmit(e){
        setConfig(username);
    }

    function renderConfig() {
        if(config) {
            let url = `/#${username}`;
            return (
                <div>
                    <a href={url} target="_blank">Tipper Link</a>
                    <iframe src={url} frameBorder={0}/>
                </div>
            );
        }
        return null;
    }

    let frag = window.location.hash;
    if(frag !== "") {
        return <TipperWidget username={frag.substr(1)}/>;
    }
    
    return (
        <div className="app">
            <h1>Hello</h1>
            <p>
                Please enter your Strike username:
            </p>
            <input type="text" placeholder="Username" onInput={handleInput} value={username}/>
            <div className={"btn"} onClick={handleSubmit}>Submit</div>
            {renderConfig()}
        </div>
    );
}
