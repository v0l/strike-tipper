import './App.css';
import {useState} from "react";

export function LNURLApp() {
    let [username, setUsername] = useState();
    let [config, setConfig] = useState();
    
    function handleInput(e) {
        setUsername(e.target.value);
    }
    
    async function handleSubmit(e){
        let i = e.target.previousElementSibling;
        let username = i.value;
        
        let cfg = await fetch(`/lnurlpay/${username}`);
        if(cfg.ok) {
            let data = await cfg.json();
            console.debug(data);
            setConfig(data);
        }
    }
    
    function renderConfig() {
        if(config) {
            return (
              <div>
                  <h3>Your QR</h3>
                  <img width="320" src={`data:image/png;base64,${config.qr}`} alt="qr"/>
                  <br/>
                  <code>{config.url}</code>
              </div>  
            );
        }
        return null;
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
