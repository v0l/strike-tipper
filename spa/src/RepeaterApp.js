import './App.css';
import {useState} from "react";
import {TipperWidget} from "./TipperWidget";

export function RepeaterApp() {
    let [username, setUsername] = useState();
    let [amount, setAmount] = useState(1);
    let [description, setDescription] = useState("Stream tip");
    let [config, setConfig] = useState();

    async function handleSubmit(e){
        setConfig({
            username, amount, description
        });
    }

    function renderConfig() {
        if (config) {
            let url = `/#${btoa(JSON.stringify(config))}`;
            return (
                <div>
                    <a href={url} target="_blank">Tipper Link</a>
                </div>
            );
        }
        return null;
    }

    let frag = window.location.hash;
    if (frag !== "") {
        let cfg = JSON.parse(atob(frag.substr(1)));
        return <TipperWidget {...cfg}/>;
    }
    
    return (
        <div className="app">
            <h1>Hello</h1>
            <dl>
                <dd>Strike username:</dd>
                <dt><input type="text" placeholder="Username" onInput={(e) => setUsername(e.target.value)} value={username} /></dt>
            
                <dd>Tip amount (USD):</dd>
                <dt><input type="number" placeholder="Amount" onInput={(e) => setAmount(e.target.value)} value={amount} /></dt>
            
                <dd>Description:</dd>
                <dt><input type="text" placeholder="Description" onInput={(e) => setDescription(e.target.value)} value={description} /></dt>
            </dl>
            <div className={"btn"} onClick={handleSubmit}>Submit</div>
            {renderConfig()}
        </div>
    );
}
