import React from 'react';
import ReactDOM from 'react-dom';
import {Provider} from 'react-redux';
import {ConfigPage} from "./ConfigPage";
import store from "./Store";

import './index.css';
import {TipperWidget} from "./TipperWidget";

const ShowApp = window.location.hash !== "";
const Config = ShowApp ? JSON.parse(atob(window.location.hash.substring(1))) : {};

ReactDOM.render(
    <React.StrictMode>
        <Provider store={store}>
            {ShowApp ? <TipperWidget {...Config}/> : <ConfigPage/>}
        </Provider>
    </React.StrictMode>,
    document.getElementById('root')
);