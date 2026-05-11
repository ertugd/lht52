// Firebase Configuration
const firebaseConfig = {
    apiKey: "AIzaSyBdf4kMeo-W7dyUpzI2B_AL_Tsj6J2Ghiw",
    authDomain: "istiklal-karacasu.firebaseapp.com",
    databaseURL: "https://istiklal-karacasu-default-rtdb.europe-west1.firebasedatabase.app",
    projectId: "istiklal-karacasu",
    storageBucket: "istiklal-karacasu.firebasestorage.app",
    messagingSenderId: "666917451355",
    appId: "1:666917451355:web:2e1535ce0a5baba87ff17e",
    measurementId: "G-9YW69WWHN4"
};

// Initialize Firebase
if (!firebase.apps.length) {
    firebase.initializeApp(firebaseConfig);
}
const database = firebase.database();
