import { configureStore } from '@reduxjs/toolkit'

import widgetReducer from "./WidgetState";

export default configureStore({
    reducer: {
        widget: widgetReducer
    },
})