<?php
$appName = '123';
$appBundle = '123';
$secretKey = '2f40082bdf0a42a1';

if (isset($_GET['key9904']) && $_GET['key9904'] === $secretKey) {
    echo 'Привет, я приложение ' . $appName . ', моя ссылка: https://play.google.com/store/apps/details?id=' . $appBundle;
}