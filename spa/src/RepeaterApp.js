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
                    <br/>
                    <h3>Add the following link as a 'Browser Source' in OBS:</h3>
                    <a href={url} target="_blank" className="widget-link">COPY ME</a>
                    <br />
                    <h3>Help</h3>
                    <p>
                        The tip widget uses a resolution of 600x700, you should set this as the resolution of the browser in OBS.
                    </p>
                    <p>
                        The tip widget will work in any alternatives to OBS as long as you can use a web page as a 'Layer'.
                    </p>
                    <p>
                        The amount is fixed and will allow people to tip ONLY the amount you specify above.
                    </p>
                    <p>
                        Works with any lightning wallet supporting BOLT11 invoices. ⚡
                    </p>
                    <p>
                        If the widget doesn't load, please check the username is correct.
                    </p>
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
